namespace NFun.InfinityProfiling.Sets
{
    public static class Scripts
    {
        public static string PrimitiveConstIntSimpleArithmetics => "out:int =(4 * 12).rema(7) + ((9 * 2) + 8)";
        public static string PrimitiveConstRealSimpleArithmetics => "(4 * 12).rema(7) + ((9 * 2) / 8)";
        public static string PrimitiveConstBoolSimpleArithmetics => "(2 == 2 * 5) and ((1 / 3.1) * 3 == 1)";
        public static string PrimitiveCalcReal2Var => "out:real =a*7+b";
        public static string PrimitiveCalcInt2Var => "out:int =a*7+b";

        public static string ConstTrue => "true";
        public static string ConstBoolOp => "true and false or not true";
        public static string Const1 => "1";
        public static string ConstText => "'let us make some fun!'";
        public static string ConstBoolArray => "y=[true, false, true, true,true,false,false]";
        public static string ConstRealArray => "y=[1,2,3,4,5,6,7,7,8,10]";

        public static string ConstInterpolation =>
            "a = 2; b =3; res =" +
            "'{a}+{b} = {a+b}\\r" +
            "{a}-{b}  = {a-b}\\r" +
            "{a}*{b}  = {a*b}\\r" +
            "{a}**{b} = {a**b}'";

        public static string ConstGenericFunc =>
            "max3(a,b,c) = a.max(b).max(c); " +
            "r = max3(1,2,3); " +
            "i = max3(0x1,0x2,0x3); " +
            "t = max3('abc','cde','fgh')";

        public static string ConstSquareEquation =>
            "a = 1; b = 10; c = 3;" +
            "d = b**2 - 4*a*c;" +
            "x = if(d<0)[];" +
            "    if(d==0)[(-b+sqrt(d))/(2*a),(-b-sqrt(d))/(2*a)];" +
            "    else [-b/(2*a)]";


        public static string CalcKxb => "y = 10 * x + 1";
        public static string CalcSingleReal => "y:real = x";
        public static string CalcSingleBool => "y:bool = x";
        public static string CalcSingleText => "y:text = x";

        public static string CalcIntOp => "y:int = x^0xF0";
        public static string CalcRealOp => "y:real = x+1";
        public static string CalcBoolOp => "y:bool = x xor false";
        public static string CalcTextOp => "y:text = 'x = {x}'";


        public static string CalcInterpolation =>
            "'{a}+{b} = {a+b}\\r" +
            "{a}-{b}  = {a-b}\\r" +
            "{a}*{b}  = {a*b}\\r" +
            "{a}**{b} = {a**b}'";

        public static string CalcGenericFunc =>
            "max3(a,b,c) = a.max(b).max(c); " +
            "max3(a,b,c); ";

        public static string CalcSquareEquation =>
            "d = b**2 - 4*a*c;" +
            "x = if(d<0)[];" +
            "    if(d==0)[(-b+sqrt(d))/(2*a),(-b-sqrt(d))/(2*a)];" +
            "    else [-b/(2*a)]";

        public static string CalcRealArray => "y=[1.0,x,x]";
        public static string CalcFourArgs => "y='{(a*b+c)}'.concat(d)";


        public static string MultiplyArrayItems =>
            @"multi(a,b) = 
                         if(a.count()!=b.count()) []
                         else
                             [0..a.count()-1].map(fun a[it]*b[it]) 

                     a =  [1,2,3]
                     b =  [4,5,6]
                     expected = [4,10,18]     
                     
                     passed = a.multi(b)==expected";

        public static string DummyBubbleSort =>
            @" 
  	                  twiceSet(arr,i,j,ival,jval) = arr.set(i,ival).set(j,jval)

                     #swap elements i,j in array arr  
                     swap(arr, i, j) 
                       = arr.twiceSet(i,j,arr[j], arr[i])
                     
                     #swap elements i, i+1 if they are not sorted
                     swapIfNotSorted(c, i)
  	                   =	if   (c[i]<c[i+1]) c
  		                   else c.swap(i, i+1)

                     # run thru array 
                     # and swap every unsorted values
                     onelineSort(input) =  
  	                   [0..input.count()-2].fold(input, swapIfNotSorted)		

                     bubbleSort(input)=
  	                   [0..input.count()-1]
  		                   .fold(input, fun onelineSort(it1))

                     i:int[]  = [1,5,3,5,6,1,2,100,0,3,2,10,3,50,6,42,43,53].bubbleSort()";


        public static string Everything =>
            @"
                          twiceSet(arr,i,j,ival,jval) = arr.set(i,ival).set(j,jval)

                          #swap elements i,j in array arr  
                          swap(arr, i, j) = arr.twiceSet(i,j,arr[j], arr[i])
                          
                          #swap elements i, i+1 if they are not sorted
                          swapIfNotSorted(c, i) = 
                                        if(c[i]< c[i+1]) c
                                        if(c[i]==c[i+1]) c 
                                        else c.swap(i, i+1)

                          # run thru array and swap every unsorted values
                          onelineSort(input) = [0..input.count()-2].fold(input, swapIfNotSorted)		

                          bubbleSort(input)  = [0..input.count()-1].fold(input, fun onelineSort(it1))

                          #body  
                          ins:int[]  = [1,5,3,5,6,0x1,2,0b100,0,3,2,10,3,50,6,42,43,53]
                          rns:real[] = ins
                          tns  = ins.filter(fun it.rema(2)==0).map(toText).concat(['vasa','kate', '21*2 = {21*2}'])
                        
                          i  = ins.bubbleSort() == ins.reverse().sort()
                          r  = rns.bubbleSort() == rns.sort()
                          t  = ('vasa' in tns) and (500 in [1..1000]) #true

                          myOr(a,b):bool = a or b  
                          k =  [0..1000].map(fun i and r or t xor i).fold(myOr)

                          mySum(a,b) = a + b  
                          j =  [0..100].map(fun (ins[1]+ it- ins[2])/sin(it)).fold(mySum);    
                          #uncomment when steps gonna be fixed
						  #m =  [0..20000..2].sum()
";
    }
}