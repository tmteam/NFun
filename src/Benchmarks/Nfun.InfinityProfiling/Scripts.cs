namespace NFun.InfinityProfiling; 

public static class Scripts {
	public const string ConstTrue = "true";
	public const string Const1 = "1";
	public const string ConstText = "'let us make some fun!'";
	public const string ConstBoolArray = "y=[true, false, true, true,true,false,false]";
	public const string ConstRealArray = "y=[1,2,3,4,5,6,7,7,8,10]";

	public const string CalcKxb = "y = 10 * x + 1";
	public const string CalcReal = "y:real = x";
	public const string CalcBool = "y:bool = x";
	public const string CalcText = "y:text = x";
	public const string CalcRealArray = "y=[1.0,x,x]";
	public const string CalcFourArgs = "y='{(a*b+c)}'.concat(d)";

	// public readonly string ConstThousandSum = "[1..1000..1.0].sum()";

	public const string MultiplyArrayItems = @"multi(a,b) = 
                              if(a.count()!=b.count()) []
                              else
                                  [0..a.count()-1].map(fun a[it]*b[it]) 

                          a =  [1,2,3]
                          b =  [4,5,6]
                          expected = [4,10,18]     
                          
                          passed = a.multi(b)==expected";

	public const string DummyBubbleSort = @" 
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
  		                        .fold(input) {onelineSort(it1)}

                          i:int[]  = [1,5,3,5,6,1,2,100,0,3,2,10,3,50,6,42,43,53].bubbleSort()";


	public const string Everything = @"
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
                          tns  = ins.filter(fun it % 2==0}.map(toText).concat(['vasa','kate', '21*2 = {21*2}'])
                        
                          i  = ins.bubbleSort() == ins.reverse().sort()
                          r  = rns.bubbleSort() == rns.sort()
                          t  = ('vasa' in tns) and (500 in [1..1000]) #true

                          myOr(a,b):bool = a or b  
                          k =  [0..1000].map(fun i and r or t xor i}.fold(myOr)

                          mySum(a,b) = a + b  
                          j =  [0..100].map(fun (ins[1]+ it- ins[2])/sin(it)}.fold(mySum);    
                          #uncomment when steps gonna be fixed
						  #m =  [0..20000..2].sum()
";
}