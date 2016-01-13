namespace MarketModels

module SolverExtensions =
    open System
    open System.Collections.Generic    
    open Microsoft.FSharp.Collections
    open Microsoft.SolverFoundation.Services
    open Microsoft.SolverFoundation.Common

    let (---) (d1 : Term, d2 : Term) = Term.op_Subtraction(d1, d2)

    let (<<==) (d1 : Decision) (x : float) = Term.op_LessThanOrEqual(d1, Term.op_Implicit(x))

    let (===) (d1 : Term) (x : float) = Term.op_Equality(d1, Term.op_Implicit(x))
    
    //must have more than 2 elements...
    let DecisionsSum (d1 : Decision[]) = 
        d1 |> Seq.skip 1 |> Seq.fold (fun acc elem -> acc + elem) (d1.[0] :> Term)

    let DecisionsSumConstProduct (d1 : Decision[]) (c : float[])= 
        let mutable acc = Term.op_Multiply(d1.[0], Term.op_Implicit(c.[0]))
        d1 
            |> Seq.skip 1 
            |> Seq.iteri (fun i x -> acc <- Term.op_Addition(acc, Term.op_Multiply(d1.[i+1], Term.op_Implicit(c.[i+1]))))
        acc

    let CumSum (arr : float[]) : float[] =        
        arr |> Array.scan (+) 0.0 |> Seq.skip 1 |> Seq.toArray
        //(fun balance transactionAmount -> balance + transactionAmount)

    let IndexOfLastLessOrEqualThan (arr : float[]) (value : float): int =        
        arr |> Array.findIndex (fun a -> a >= value)
