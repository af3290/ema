namespace MarketModels

module Operations =
    //between arrays
    let inline (.+) a b = Array.map2 (+) a b
    let inline (.-) a b = Array.map2 (-) a b
    let inline (./) a b = Array.map2 (/) a b
    let inline (.*) a b = Array.map2 (*) a b

    //between array and scalar
    let inline (^^) a n = a |> Array.map (fun x -> x ** n)
    let inline (.+.) a value = a |> Array.map (fun x -> x + value)
    let inline (.-.) a value = a |> Array.map (fun x -> x - value)
    let inline (.*.) a value = a |> Array.map (fun x -> x * value)
    let inline (./.) a value = a |> Array.map (fun x -> x / value)
