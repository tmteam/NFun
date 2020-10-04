using NFun;
using NUnit.Framework;

namespace Funny.Tests
{
    public class ComparationTest
    {
        [TestCase("true == false", false)]
        [TestCase("true==true==1", false)]
        [TestCase("8==8==1", false)]
        [TestCase("true==true==true", true)]
        [TestCase("8==8==8", false)]
        
        [TestCase("[0,0,1]!=[0,false,1]",true)]
        [TestCase("[0,0,1]==[0,false,1]", false)]

        [TestCase("[false,0,1]!=[0,false,1]", true)]

        [TestCase("[false,0,1,'vasa',[1,2,3]]==[false,0,1,'vasa',[1,2,3]]", true)]
        [TestCase("[false,0,1,'vasa',[1,2,3]]!=[false,0,1,'vasa',[1,2,3]]", false)]

        [TestCase("[false,0,1,'peta',[1,2,3]]==[false,0,1,'vasa',[1,2,3]]", false)]
        [TestCase("[false,0,1,'peta',[1,2,3]]!=[false,0,1,'vasa',[1,2,3]]", true)]

        [TestCase("[false,0,1,'vasa',[1,2,[1,2]]]==[false,0,1,'vasa',[1,2,[1,2]]]", true)]
        [TestCase("[false,0,1,'vasa',[1,2,[10000,2]]]==[false,0,1,'vasa',[1,2,[1,2]]]", false)]
        [TestCase("[false,0,1,'vasa',[1,2,[10000,2]]]!=[false,0,1,'vasa',[1,2,[1,2]]]", true)]


        [TestCase("[false,0,1]==[false,0,1]", true)]
        [TestCase("[false,0,1]!=[false,0,1]", false)]
        [TestCase("[false,0,1]==[0,false,1]", false)]
        [TestCase("[false,0,1]!=[0,false,1]", true)]

        [TestCase("[0,0,1]==[0,0,1]", true)]
        [TestCase("[0,0,1]!=[0,0,1]", false)]
        
        [TestCase("[0,1,1]==[0,0,1]", false)]
        [TestCase("[0,1,1]!=[0,0,1]", true)]

        [TestCase("[0,0,1.0]==[0,0,1]", true)]
        [TestCase("[0,0,1]!=[0,0,1.0]", false)]

        [TestCase("[0,1.0,1]==[0,0,1]", false)]
        [TestCase("[0,1,1]!=[0,0.0,1]", true)]

        [TestCase("[false,true, false]==[false,true, false]", true)]
        [TestCase("[false,true, false]!=[false,true, false]", false)]


        [TestCase("0 == 0 == 8", false)]
        [TestCase("8 == 1 == 0", false)]
        [TestCase("true == 1", false)]
        [TestCase("1==1.0", true)]
        [TestCase("0==0.0", true)]
        [TestCase("1==1", true)]
        [TestCase("1==0", false)]
        [TestCase("true==true", true)]
        [TestCase("true==false", false)]

        [TestCase("'a'[0]=='b'[0] ", false)]
        [TestCase("'a'[0]!='b'[0] ", true)]
        [TestCase("'a'[0]== 'a'[0] ", true)]
        [TestCase("'a'[0]!='a'[0] ", false)]
        
        [TestCase("'avatar'== 'bigben' ", false)]
        [TestCase("'avatar'!= 'bigben' ", true)]
        public void Equal(string expr, object expected)
            => FunBuilder.Build(expr).Calculate().AssertOutEquals(expected);

        [TestCase("1!=0", true)]
        [TestCase("0!=1", true)]
        [TestCase("5!=5", false)]
        [TestCase("5>5", false)]
        [TestCase("5>3", true)]
        [TestCase("5>6", false)]
        [TestCase("5>=5", true)]
        [TestCase("5>=3", true)]
        [TestCase("5>=6", false)]
        [TestCase("5<=5", true)]
        [TestCase("5<=3", false)]
        [TestCase("5<=6", true)]
        [TestCase("'a'[0]< 'b'[0] ", true)]
        [TestCase("'a'[0]<='b'[0] ", true)]
        [TestCase("'a'[0]>='b'[0] ", false)]
        [TestCase("'a'[0]> 'b'[0] ", false)]
        [TestCase("'a'[0]< 'a'[0] ", false)]
        [TestCase("'a'[0]<='a'[0] ", true)]
        [TestCase("'a'[0]>='a'[0] ", true)]
        [TestCase("'a'[0]> 'a'[0] ", false)]
        [TestCase("'avatar'< 'bigben' ", true)]
        [TestCase("'avatar'<= 'bigben' ", true)]
        [TestCase("'avatar'>= 'bigben' ", false)]
        [TestCase("'avatar'>  'bigben' ", false)]
        [TestCase("'avatar'< 'avatar' ", false)]
        [TestCase("'avatar'<= 'avatar' ", true)]
        [TestCase("'avatar'>= 'avatar' ", true)]
        [TestCase("'avatar'>  'avatar' ", false)]

        public void Compare(string expr, bool expected)
            => FunBuilder.Build(expr).Calculate().AssertOutEquals(expected);


    }
}
