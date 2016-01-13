namespace MarketModels

module Operations =
    let inline (++) a b = Array.map2 (+) a b
    let inline (--) a b = Array.map2 (+) a b
    let inline (./) a b = Array.map2 (/) a b
    let inline (^^) a n = a |> Array.map (fun x -> x ** n)
