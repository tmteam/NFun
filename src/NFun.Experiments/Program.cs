using System;
using System.Collections;
using NFun;

namespace NFun.Experiments
{
    class Program
    {
        public static readonly string Everything = @"
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
                          tns  = ins.filter{it.rema(2)==0}.map(toText).concat(['vasa','kate', '21*2 = {21*2}'])
                        
                          i  = ins.bubbleSort() == ins.reverse().sort()
                          r  = rns.bubbleSort() == rns.sort()
                          t  = ('vasa' in tns) and (500 in [1..1000]) #true

                          myOr(a,b):bool = a or b  
                          k =  [0..1000].map{i and r or t xor i}.fold(myOr)

                          mySum(a,b) = a + b  
                          j =  [0..100].map{(ins[1]+ it- ins[2])/sin(it)}.fold(mySum);    
                          #uncomment when steps gonna be fixed
						  #m =  [0..20000..2].sum()
";
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var ex1 = "10*x*x + 12*x + 1";
            var ex2 = "if(a>0) 10*x*x + 12*x + 1 else 0";
            while (true)
            {
                FunBuilder.Build(Everything);
            }
        }
    }
}