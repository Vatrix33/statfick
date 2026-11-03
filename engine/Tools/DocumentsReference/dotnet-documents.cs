// What .NET gives as the Documents folder in the same environment as an Electron probe of main.cjs
// (built and run by Tools/Scripts/make-documents-reference.sh): for the comparison in
// Docs/Assets.md, "Character files". Prints one JSON line. Without DoNotVerify .NET gives "" for a
// folder that doesn't exist. (A file-based app has reflection-based JSON serialization turned off,
// hence the writer.)
using System.Text;
using System.Text.Json;

var buffer = new MemoryStream();
using (var json = new Utf8JsonWriter(buffer))
{
    json.WriteStartObject();
    json.WriteString("documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify));
    json.WriteEndObject();
}
Console.WriteLine("DOCUMENTS-REFERENCE " + Encoding.UTF8.GetString(buffer.ToArray()));
