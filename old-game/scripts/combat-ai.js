/* Tactical policy, separate from combat mechanics. Pure forecasts and bounded,
 * serialized memory: previews never spend resources, change state, or roll RNG. */
(function (root) {
  'use strict';
  const version = 3;
  const finite = value => Number.isFinite(value) ? value : 0;
  const clamp = (value, low, high) => Math.max(low, Math.min(high, finite(value)));
  const ratio = (value, maximum) => clamp(value / Math.max(1, maximum), 0, 1);
  const actionKey = candidate => candidate.key || candidate.spell?.id || candidate.item?.id || candidate.stance || candidate.kind;
  function memory(saved) {
    return {
      version, focusId: typeof saved?.focusId === 'string' ? saved.focusId : null,
      planKey: typeof saved?.planKey === 'string' ? saved.planKey : null,
      history: (Array.isArray(saved?.history) ? saved.history : []).filter(entry => entry && typeof entry.key === 'string')
        .slice(-16).map(entry => ({ key: entry.key, kind: entry.kind, turn: finite(entry.turn), phase: finite(entry.phase), targetId: entry.targetId || null, damage: Math.max(0, finite(entry.damage)) })),
    };
  }
  function remember(saved, candidate, context, damage = 0) {
    const result = memory(saved);
    result.history.push({ key: actionKey(candidate), kind: candidate.kind, turn: context.turn, phase: context.phase,
      targetId: candidate.target?.id || null, damage: Math.max(0, finite(damage)) });
    result.history = result.history.slice(-16);
    if (candidate.tactics?.hostileTarget) result.focusId = candidate.target?.id || null;
    if (candidate.tactics?.setup && candidate.tactics.followupKey) result.planKey = candidate.tactics.followupKey;
    else if (actionKey(candidate) === result.planKey || candidate.kind === 'spell' && result.planKey !== 'attack') result.planKey = null;
    return result;
  }
  function intent(context, candidates) {
    if (ratio(context.hp, context.maxHp) < 0.35 && context.pressure > context.hp * 0.5
        && candidates.some(candidate => candidate.tactics?.healing > 0)) return 'survive';
    if (candidates.some(candidate => candidate.tactics?.killChance >= 0.8 && candidate.tactics?.damage > 0)) return 'finish';
    if (context.allyEmergency && candidates.some(candidate => candidate.tactics?.healing > 0)) return 'rescue';
    if (context.pressure >= context.hp * 0.85 && ratio(context.hp, context.maxHp) < 0.6) return 'survive';
    if (ratio(context.mana, context.maxMana) < 0.25 && context.spellCount > 0) return 'recover';
    if (context.role === 'support') return 'support';
    if (context.enemyCount >= 3) return 'pressure';
    return 'focus';
  }
  function rank(input, context) {
    const previous = memory(context.memory);
    const mode = intent(context, input);
    const useful = input.filter(candidate => candidate && Number.isFinite(candidate.score) && candidate.score > 0);
    const damaging = useful.filter(candidate => candidate.tactics?.damage > 0);
    const strongest = Math.max(0, ...damaging.map(candidate => candidate.score));
    const scored = useful.map(candidate => {
      const tactics = candidate.tactics || {};
      const ap = Math.max(0, finite(tactics.ap));
      const key = actionKey(candidate);
      const history = previous.history.filter(entry => entry.phase === context.phase);
      const thisTurn = history.filter(entry => entry.turn === context.turn && entry.key === key).length;
      const recent = history.slice(-6).filter(entry => entry.key === key).length;
      const reasons = [];
      let score = candidate.score;
      const lethal = tactics.killChance >= 0.8;
      const emergencyHeal = tactics.healing >= Math.max(5, finite(tactics.recipientMaxHp || context.maxHp) * 0.03) && tactics.recipientHealth < 0.35;
      const committed = key === previous.planKey && mode !== 'rescue';

      // Repetition is not a cooldown. Finishers, emergency heals, and creatures
      // with only one useful attack may repeat it. Otherwise prefer useful new
      // pressure/control/support instead of six copies of the same spell.
      if (candidate.kind === 'spell' && !lethal && !emergencyHeal && !committed) {
        const alternative = damaging.some(other => actionKey(other) !== key && other.score >= strongest * 0.3)
          || useful.some(other => actionKey(other) !== key && other.tactics?.support && other.score >= candidate.score * 0.45);
        if (alternative && (thisTurn || recent >= 2)) {
          score *= 1 / (1 + thisTurn * 1.9 + Math.max(0, recent - 1) * 0.45);
          reasons.push('alternate useful abilities');
        }
      }
      if (committed) { score *= 1.35; reasons.push('use the prepared follow-up'); }
      if (candidate.kind === 'attack' && context.role === 'battlemage' && thisTurn >= 1
          && useful.some(other => other.kind === 'spell' && other.tactics?.damage > 0 && other.score >= candidate.score * 0.45)) {
        score *= 0.68; reasons.push('mix weapon and magic pressure');
      }
      if (tactics.hostileTarget && candidate.target) {
        const retained = candidate.target.id === previous.focusId || candidate.target.id === context.teamFocus;
        if (retained) score *= 1.08;
        if (tactics.healerTarget) score *= 1.08;
        if (tactics.targetActingSoon) score *= 1.06;
        if (lethal) { score *= 1.5; reasons.push('remove an enemy before it acts'); }
        if (tactics.damage <= 0 && !tactics.control) score *= 0.05;
      }
      const manaRatio = ratio(context.mana, context.maxMana);
      if (candidate.kind === 'spell' && tactics.mana > 0 && !lethal && !emergencyHeal) {
        const fraction = tactics.mana / Math.max(1, context.mana);
        score *= Math.max(0.35, 1 - fraction * (manaRatio < 0.35 ? 0.7 : 0.2));
        if (manaRatio < 0.25) reasons.push('conserve remaining Mana');
      }
      if (candidate.kind === 'meditate' || tactics.manaRecovery) {
        score *= mode === 'recover' ? 1.55 : 0.55;
        reasons.push('recover useful resources');
      }
      if (tactics.healing > 0) {
        score *= mode === 'rescue' ? 1.65 : mode === 'survive' ? 1.6 : emergencyHeal ? 1.4 : tactics.recipientHealth > 0.8 ? 0.35 : 1;
        if (mode === 'rescue' || emergencyHeal) reasons.push(tactics.selfHeal ? 'recover from dangerous wounds' : 'save a wounded ally');
      }
      if (tactics.support && mode === 'support') score *= 1.15;
      if (tactics.control && tactics.freshControl) { score *= 1.13; reasons.push('disrupt a dangerous opponent'); }
      if (candidate.kind === 'stance') {
        if (mode === 'finish' && context.enemyCount === 1) score *= 0.1;
        else if (mode === 'survive') score *= 1.25;
        reasons.push('reduce forecast incoming damage');
      }
      if (tactics.setup && tactics.followupKey) {
        // Reserve enough AP for the action this charge actually improves.
        const prepared = tactics.followup;
        const followup = prepared?.key === tactics.followupKey && prepared.ap <= context.ap - ap && prepared.mana + finite(tactics.mana) <= context.mana
          ? prepared : useful.find(other => actionKey(other) === tactics.followupKey && other.tactics?.ap <= context.ap - ap && finite(other.tactics?.mana) + finite(tactics.mana) <= context.mana);
        if (!followup) score = 0;
        else { score *= 1.08; reasons.push('prepare a usable follow-up'); }
      }
      if (ap > 0) {
        const left = context.ap - ap;
        const followup = useful.filter(other => other !== candidate && other.tactics?.ap > 0 && other.tactics.ap <= left
          && other.tactics.mana + tactics.mana <= context.mana && (!tactics.hostileTarget || !lethal || other.target?.id !== candidate.target?.id))
          .sort((a, b) => b.score - a.score)[0];
        // Cheap two-step lookahead, not a branching mutation of the battlefield.
        // Reward usable leftover AP, not impossible extra copies of a buff.
        if (followup) score += Math.min(score * 0.2, followup.score * 0.16);
        else if (left > 0 && left < 2 && context.ap >= 4 && !lethal) score *= 0.92;
      }
      if (tactics.risk > 0) score -= tactics.risk;
      return { ...candidate, score, reason: reasons.join('; ') || `${context.role}: ${mode}`, intent: mode };
    }).filter(candidate => candidate.score > 0 && Number.isFinite(candidate.score));
    const priority = { skill: 5, item: 4, spell: 3, attack: 2, stance: 1, meditate: 0 };
    return scored.sort((left, right) => right.score - left.score || (priority[right.kind] || 0) - (priority[left.kind] || 0)
      || String(actionKey(left) + (left.target?.id || '')).localeCompare(String(actionKey(right) + (right.target?.id || ''))));
  }
  root.CombatAI = { version, actionKey, memory, remember, rank, intent };
  if (typeof module !== 'undefined') module.exports = root.CombatAI;
})(globalThis);
