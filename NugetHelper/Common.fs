namespace NugetHelper

open System.IO
open Pinicola.FSharp.SpectreConsole

[<AutoOpen>]
module Common =

    let searchProjects () =
        Directory.GetFiles(".", @"*.?sproj", SearchOption.AllDirectories)
        |> Seq.map (fun path -> Path.GetRelativePath(".", path))
        |> Seq.toList

    let select what whatPlural search =

        let options: string list =
            AnsiConsole.status ()
            |> Status.start $"Getting {whatPlural}..." (fun _ -> search ())

        match options with
        | [] -> Error $"No {whatPlural} found"
        | [ option ] ->
            AnsiConsole.markupLine $"Found 1 {what}: [yellow]{option}[/]"
            Ok option

        | _ ->

            let selectedOption =
                SelectionPrompt.init ()
                |> SelectionPrompt.setTitle $"Found {options.Length} {whatPlural} :"
                |> SelectionPrompt.addChoices options
                |> AnsiConsole.prompt

            AnsiConsole.markupLine $"Selected {what}: [yellow]{selectedOption}[/]"

            Ok selectedOption
