namespace MarketModels

module Optimization =
    open System
    open Operations
    open MathNet.Numerics.LinearAlgebra
    open MathNet.Numerics.LinearAlgebra.Double
    open MathNet.Numerics.LinearAlgebra.Matrix
    open MathNet.Numerics.LinearAlgebra.MatrixExtensions
    open MathNet.Numerics.LinearAlgebra.DenseMatrix    

    ///Function corresponding to matlab's lsqlin, relying on alglib's quadprogramming core method, simplified,
    ///It just restates the problem in quadratic terms, uses 'medium-scale: active-set' approach.
    let ConstrainedLinearLeastSquares (C : float[,]) (d : float[]) (Aeq : float[,]) (beq : float[]) : float[] = 
        //the problem's number of variables
        let n = d.Length
        
        //simple input validation
        if C.GetLength(0) <> C.GetLength(1) then
            failwith "Must be square matrix"
        if n <> d.Length || n <> Aeq.GetLength(1) || Aeq.GetLength(0) <> beq.Length then
            failwith "Input dimensions don't agree"

        (* restate the problem in qp term *)
        //See http://math.stackexchange.com/questions/869204/are-constrained-linear-least-squares-and-quadratic-programming-the-same-thin
        //quadratic term, C*C'
        let A = DenseMatrix.init n n (fun i j -> C.[i, j])        
        let qp_a = array2D (A.TransposeThisAndMultiply(A).ToRowArrays())
        //linear term, -C'*d
        let b = DenseVector.init n (fun i -> -d.[i])        
        let qp_b = A.TransposeThisAndMultiply(b).ToArray()
        //linear constraints are passed as single matrix, merge Aeq with beq
        let qp_c = Array2D.init (Aeq.GetLength(0)) (Aeq.GetLength(1)+1) (fun i j -> if j < Aeq.GetLength(1) then Aeq.[i, j] else beq.[i])
        //linear constraint types, all equalities in our case
        let lc = Array.init beq.Length (fun i -> 0)
        //set scale
        let s = Array.init n (fun i -> 1.0)                
        //set bounds
        let lb = Array.init n (fun i -> -infinity)
        let ub = Array.init n (fun i -> infinity)

        (* optimization procedure *)
        let mutable (state : alglib.minqpstate) = null;
        let mutable (rep : alglib.minqpreport) = null;
        let mutable x0 = Array.init n (fun i -> 0.0)

        alglib.minqpcreate(n, &state);
        alglib.minqpsetstartingpoint(state, x0);
        alglib.minqpsetquadraticterm(state, qp_a);
        alglib.minqpsetlinearterm(state, qp_b);
        alglib.minqpsetlc(state, qp_c, lc);
        alglib.minqpsetbc(state, lb, ub);
        
        alglib.minqpsetscale(state, s);        
        alglib.minqpsetalgobleic(state, 0.0, 0.0, 0.0, 0);
        alglib.minqpoptimize(state);
        alglib.minqpresults(state, &x0, &rep);

        x0
       
    let ConstrainedMultivariateWithBounds (funcParams : float[]) (funcParamsBounds : float[,]) (func : alglib.ndimensional_func) : float[] =
        let funcNbParams = funcParams.Length
        
        //initial parameters values, updated with the optimal values afterwards
        let mutable values = Array.init funcNbParams (fun i -> funcParams.[i])
        let mutable state : alglib.minbleicstate = null
        let mutable rep : alglib.minbleicreport = null
        
        let bndl = funcParamsBounds.[0, *]
        let bndu = funcParamsBounds.[1, *]

        alglib.minbleiccreatef(funcNbParams, values, 1.0e-6, &state)
        alglib.minbleicsetbc(state, bndl, bndu)
        alglib.minbleicsetcond(state, 0.0000000001, 0.0, 0.0, 0);
        alglib.minbleicoptimize(state, func, null, null);
        alglib.minbleicresults(state, &values, &rep);
        
        values
