namespace MarketModels

module PrincipalComponentAnalysis =
    open System
//    open alglibnet2

    type PCAResult = {
        Coeff : float[]
        Var : float[] //because the inners have the same lengths! not array of arrays!
    }

    let PerformPCA (data : float[,]) : PCAResult =
        let res = {
                Coeff =  [|1.0|];
                Var = [|1.0|]
            }

        res
    
    //then perform pca on forward contracts... yes...

