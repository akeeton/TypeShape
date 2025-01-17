﻿module TypeShape.HKT.Clone

open System

// HKT encoding for prettyprinter types
type Cloner =
    interface HKT
    static member Assign(_ : App<Cloner, 'a>, _ : 'a -> 'a) = ()

type FieldCloner =
    interface HKT
    static member Assign(_ : App<FieldCloner, 'a>, _ : 'a -> 'a -> 'a) = ()

type ClonerBuilder() =
    interface IFSharpTypeBuilder<Cloner, FieldCloner> with
        member __.Unit() = HKT.pack id
        member __.Bool () = HKT.pack id
        member __.Int32 () = HKT.pack id
        member __.Int64 () = HKT.pack id
        member __.String () = HKT.pack String.Copy

        member __.Guid () = HKT.pack id
        member __.TimeSpan () = HKT.pack id
        member __.DateTime () = HKT.pack id
        member __.DateTimeOffset() = HKT.pack id

        member __.Option (HKT.Unpack ec) = HKT.pack(Option.map ec)
        member __.Array (HKT.Unpack ec) = HKT.pack(Array.map ec)
        member __.List (HKT.Unpack ec) = HKT.pack(List.map ec)
        member __.Set (HKT.Unpack ec) = HKT.pack(Set.map ec)
        member __.Map (HKT.Unpack kc) (HKT.Unpack vc) = HKT.pack(Map.toSeq >> Seq.map (fun (k,v) -> kc k, vc v) >> Map.ofSeq)

        member __.Field shape (HKT.Unpack fc) = 
            HKT.pack(fun src tgt -> shape.Set tgt (fc (shape.Get src)))

        member __.Tuple shape (HKT.Unpacks fields) =
            HKT.pack(fun t ->
                let mutable t' = shape.CreateUninitialized()
                for f in fields do t' <- f t t'
                t')

        member __.Record shape (HKT.Unpacks fields) =
            HKT.pack(fun t ->
                let mutable t' = shape.CreateUninitialized()
                for f in fields do t' <- f t t'
                t')

        member __.Union shape (HKT.Unpackss fieldss) =
            let tag,case = shape.UnionCases |> Seq.mapi (fun i c -> (i,c)) |> Seq.minBy (fun (_,c) -> c.Arity)
            let fields = fieldss.[tag]
            HKT.pack(fun t ->
                let mutable t' = case.CreateUninitialized()
                for f in fields do t' <- f t t'
                t')

        member __.Delay cell = HKT.pack(fun t -> let f = HKT.unpack cell.Value in f t)

let mkCloner<'t> () : 't -> 't = FSharpTypeBuilder.fold (ClonerBuilder()) |> HKT.unpack 
let clone t = mkCloner<'t>() t

//----------------------

open System

type P = Z | S of P

clone (42, Some "42", {| x = 2 ; y = S(S(S Z)) |})