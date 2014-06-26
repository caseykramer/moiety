#I @"tools\fake"
#r "FakeLib.dll"

open Fake

let project = "Moiety"
let authors = ["Casey Kramer"]
let summary = "Moiety - A parser for delimited text data (files or streams)"
let description = "Supports using any string as a field delimiter, or row delimiter, quoted fields, and the ability to handle unicode reasonably well."
let version = "2.0.0.1"
let tags = "f# c# csv parsing text"
let nugetDir = @".\nuget"

let buildDir = @".\build\"
let testDir =  @".\test\"

let nunitPath = @".\tools\NUnit"

let appReferences  = 
    !+ @"src\Moiety\**\*.fsproj" |> Scan
    
let testReferences = 
    !+ @"src\tests\**\*.fsproj" |> Scan

Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir]
)

Target "BuildApp" (fun _ ->
    MSBuildRelease buildDir "Build" appReferences
        |> Log "AppBuild-Output:"
)

Target "BuildTest" (fun _ ->
    MSBuildRelease testDir "Build" testReferences
        |> Log "TestBuild-Output:"
)

Target "Test" (fun _ ->
 !! (testDir + @"\*.dll") 
        |> NUnit (fun p -> 
            {p with 
                ToolPath = nunitPath; 
                OutputFile = testDir + @"TestResults.xml"})
)

Target "Package" (fun _ ->
    let nugetPath = "tools/nuget/NuGet.exe"
    let nugetBuildDir = nugetDir @@ "build"

    CopyDir nugetBuildDir buildDir allFiles
    
    NuGet (fun p ->
        { p with
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = version
            OutputPath = nugetDir
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"})  "moiety.nuspec"
)
  
Target "All" DoNothing

"Clean"
    ==> "BuildApp"
    ==> "BuildTest"
    ==> "Test"
    ==> "Package"
    ==> "All"

Run <| getBuildParamOrDefault "target" "All"