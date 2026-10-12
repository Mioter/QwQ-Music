$combinations = @(
    @("linux-x64", "net10.0"),
    @("win-x64", "net10.0-windows10.0.19041.0")
)
foreach ($platform in $combinations)
{
    $rid = $platform[0]
    $framework = $platform[1]
    dotnet publish `
    "..\QwQ Music\QwQ Music.csproj" `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:PublishTrimmed=true `
    -p:TrimMode=full `
    -p:PublishReadyToRun=true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:SelfContained=true `
    -c Release `
    -f $framework `
    -r $rid `
    -o "G:/Publish/QwQ Music/$rid"
}