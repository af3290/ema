namespace MarketModels

module JaggedArray =
    open System

    let transpose jaggedArray = 
        let cols = Array.length jaggedArray
        let rows = Array.length jaggedArray.[0]
        Array.init rows (fun i -> Array.init cols (fun j -> jaggedArray.[j].[i]))

    let to2DArray jaggedArray = 
        let rows = Array.length jaggedArray
        let cols = Array.length jaggedArray.[0]
        Array2D.init rows cols (fun i j -> jaggedArray.[i].[j])

    let fromArray2D array2D = 
        let rows = Array2D.length1 array2D 
        let cols = Array2D.length2 array2D
        Array.init rows (fun i -> Array.init cols (fun j -> array2D.[i,j]))

//    let fromMatrix (matrix : Matrix) = 
//        matrix.Rows |> Seq.map(fun vector -> vector.ToArray()) |> Seq.toArray

    let print jaggedArray = 
        jaggedArray |> Array.iter (fun valuei ->  
                                        printfn "%s" "" |> ignore
                                        valuei |> Array.iteri(fun j valuej -> printf "%e %s" valuej " ") |> ignore)

    let alignInnerArrays jaggedArray =
        let shortestLength = jaggedArray |> Array.fold (fun state arr -> min (arr |> Array.length) state) (Int32.MaxValue)
        jaggedArray |> Array.map (fun arr -> Array.init shortestLength (fun i -> arr.[i]))

    let toArray jaggedArray = 
        jaggedArray |> Array.concat

    let map mapping jaggedArray = 
        jaggedArray |> Array.map(fun arr -> Array.map mapping arr)

