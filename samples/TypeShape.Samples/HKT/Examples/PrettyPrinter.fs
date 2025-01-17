﻿module TypeShape.HKT.PrettyPrinter

// HKT encoding for prettyprinter types
type PrettyPrinter =
    interface HKT
    static member Assign(_ : App<PrettyPrinter, 'a>, _ : 'a -> string) = ()

type PrettyPrinterBuilder() =
    interface IFSharpTypeBuilder<PrettyPrinter, PrettyPrinter> with
        member __.Unit() = HKT.pack(fun () -> "()")
        member __.Bool () = HKT.pack(function true -> "true" | false -> "false")
        member __.Int32 () = HKT.pack(fun i -> i.ToString())
        member __.Int64 () = HKT.pack(fun i -> i.ToString())
        member __.String () = HKT.pack(fun s -> sprintf "\"%s\"" s)

        member __.Guid () = HKT.pack(fun g -> g.ToString("N"))
        member __.TimeSpan () = HKT.pack(fun t -> t.ToString())
        member __.DateTime () = HKT.pack(fun d -> d.ToString("O"))
        member __.DateTimeOffset() = HKT.pack(fun d -> d.ToString("O"))

        member __.Option (HKT.Unpack ep) = HKT.pack(function None -> "None" | Some x -> sprintf "Some(%s)" (ep x))
        member __.Array (HKT.Unpack ep) = HKT.pack(fun xs -> xs |> Seq.map ep |> String.concat "; " |> sprintf "[|%s|]")
        member __.List (HKT.Unpack ep) = HKT.pack(fun xs -> xs |> Seq.map ep |> String.concat "; " |> sprintf "[%s]")
        member __.Set (HKT.Unpack ep) = HKT.pack(fun xs -> xs |> Seq.map ep |> String.concat "; " |> sprintf "set [%s]")
        member __.Map (HKT.Unpack kp) (HKT.Unpack vp) = 
            HKT.pack(fun xs -> 
                xs 
                |> Map.toSeq 
                |> Seq.map (fun (k,v) -> sprintf "(%s, %s)" (kp k) (vp v)) 
                |> String.concat ";" 
                |> sprintf "map [%s]")

        member __.Field shape (HKT.Unpack fp) = HKT.pack(fun t -> fp (shape.Get t))

        member __.Tuple shape (HKT.Unpacks fields) =
            let fmtBracket =
                if shape.IsStructTuple 
                then sprintf "struct(%s)"
                else sprintf "(%s)"

            HKT.pack(fun tuple ->
                fields
                |> Seq.map (fun f -> f tuple)
                |> String.concat ", "
                |> fmtBracket)

        member __.Record shape (HKT.Unpacks fields) =
            let fmtBracket = 
                match shape.IsAnonymousRecord, shape.IsStructRecord with
                | true, true -> sprintf "struct {| %s |}"
                | true, false -> sprintf "{| %s |}"
                | _ -> sprintf "{ %s }"

            HKT.pack(fun record ->
                fields
                |> Seq.zip shape.Fields
                |> Seq.map (fun (f,fp) -> f.Label, fp record)
                |> Seq.map (fun (label, value) -> sprintf "%s = %s" label value)
                |> String.concat "; "
                |> fmtBracket)

        member __.Union shape (HKT.Unpackss fieldss) =
            HKT.pack (fun union ->
                let tag = shape.GetTag union
                let case = shape.UnionCases.[tag]
                if case.Fields.Length = 0 then case.CaseInfo.Name
                else
                    fieldss.[tag]
                    |> Seq.map (fun f -> f union)
                    |> String.concat ", "
                    |> sprintf "%s(%s)" case.CaseInfo.Name)

        member __.Delay f = HKT.pack(fun x -> HKT.unpack f.Value x)

let mkPrinter<'t> () : 't -> string = FSharpTypeBuilder.fold (PrettyPrinterBuilder()) |> HKT.unpack


//----------------------

open System

type P = Z | S of P

let p = mkPrinter<int * (string * DateTime) option * {| x : int ; p : P |}>()

p (42, Some ("42", DateTime.Now), {| x = 42 ; p = S(S(S Z)) |})