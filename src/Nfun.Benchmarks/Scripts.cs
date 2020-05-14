namespace Nfun.Benchmarks
{
    public class Scripts
    {
        public readonly string VarKxb = "y = 10 * x + 1";
        public readonly string ConstKxb = "y = 10 * 100.0 + 1";
        public readonly string Const1 = "1";
        public readonly string ConstTrue = "true";
        public readonly string ConstText = "'let us make some fun!'";
        public readonly string ConstBoolArray = "[true, true, false, true, true, false, false]";
        public readonly string ConstRealArray = "[1, 2, 3, 4, 5, 6, 7]";


        public readonly string ConstThousandSum = "[1..1000..1.0].sum()";

        public readonly string MultiplyArrayItems = @"multi(a,b) = 
                              if(a.count()!=b.count()) []
                              else
                                  [0..a.count()-1].map(i->a[i]*b[i])  

                          a =  [1,2,3]
                          b =  [4,5,6]
                          expected = [4,10,18]     
                          
                          passed = a.multi(b)==expected";
        public readonly string DummyBubbleSort = @" 
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
  		                        .fold(
  			                        input, 
  			                        (c,i)-> c.onelineSort())

                          i:int[]  = [1,5,3,5,6,1,2,100,0,3,2,10,3,50,6,42,43,53].bubbleSort()";
    }
}