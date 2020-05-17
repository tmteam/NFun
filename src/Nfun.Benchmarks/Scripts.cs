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
                                  [0..a.count()-1].map{a[it]*b[it]}  

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
  		                        .fold(input) {onelineSort(it1)}

                          i:int[]  = [1,5,3,5,6,1,2,100,0,3,2,10,3,50,6,42,43,53].bubbleSort()";


        public readonly string Everything = @"
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

                          bubbleSort(input)  = [0..input.count()-1].fold(input) {onelineSort(it1)}

                          #body  
                          ins:int[]  = [1,5,3,5,6,0x1,2,0b100,0,3,2,10,3,50,6,42,43,53]
                          rns:real[] = ins
                          tns  = ins.filter{it%2==0}.map(toText).concat(['vasa','kate', '21*2 = {21*2}'])
                        
                          i  = ins.bubbleSort() == ins.reverse().sort()
                          r  = rns.bubbleSort() == rns.sort()
                          t  = ('vasa' in tns) and (500 in [1..1000]) #true

                          myOr(a,b):bool = a or b  
                          k =  [0..1000].map{i and r or t xor i}.fold(myOr)

                          mySum(a,b) = a + b  
                          j =  [0..100].map{(ins[1]+ it- ins[2])/sin(it)}.fold(mySum);    
                          m =  [0..20000..2].sum()
";

    }
}