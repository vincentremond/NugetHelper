namespace NugetHelper

open System
open System.IO
open Pinicola.FSharp.SpectreConsole

[<AutoOpen>]
module Common =

    let searchProjects () =
        Directory.GetFiles(".", @"*.?sproj", SearchOption.AllDirectories)
        |> Seq.map (fun path -> Path.GetRelativePath(".", path))
        |> Seq.toList

    let private consoleLock = Object()

    let writeToConsole color =
        fun (s: string) ->
            match s |> Option.ofObj with
            | Some s -> lock consoleLock (fun _ -> AnsiConsole.markupLineInterpolated $"[{color}]{s}[/]")
            | None -> ()

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
                |> SelectionPrompt.withRawTitle $"Found {options.Length} {whatPlural} :"
                |> SelectionPrompt.addChoices options
                |> AnsiConsole.prompt

            AnsiConsole.markupLine $"Selected {what}: [yellow]{selectedOption}[/]"

            Ok selectedOption
