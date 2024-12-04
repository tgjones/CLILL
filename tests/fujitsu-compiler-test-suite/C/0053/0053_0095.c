#include<stdio.h>

 volatile long int a4;
 const int long a5;
int main()
{

   const long int *addrc1,*addrc2;
   volatile long int *addrv1,*addrv2;

   printf("***  ** MK3 TEST FOR DCL ** STARTED ***\n");

   {
     auto const long int a1=0;
     register volatile int long a2=0,b2=1;
     static const long int a3[2]={2,-3};
     extern volatile long int a4;
     extern const int long a5;
     typedef volatile long int type1;
     type1  a6=0;


     addrc1 = &a1;
     if (a1!=*addrc1)
         printf(" ***  TEST-01 ERROR ***\n");
     else
         printf(" ***  TEST-01 OK ***\n");

     if (a2 || b2!=1)
         printf(" ***  TEST-02 ERROR ***\n");
     else
         printf(" ***  TEST-02 OK ***\n");

     if (a3[0]!=2 || a3[1]!=-3)
         printf(" ***  TEST-03 ERROR ***\n");
     else
         printf(" ***  TEST-03 OK ***\n");

     if (a4!=0)
         printf(" ***  TEST-04 ERROR ***\n");
     else
         printf(" ***  TEST-04 OK ***\n");

     if (a5!=0)
         printf(" ***  TEST-05 ERROR ***\n");
     else
         printf(" ***  TEST-05 OK ***\n");

     addrv1 = &a6;
     if (a6!=*addrv1)
         printf(" ***  TEST-06 ERROR ***\n");
     else
         printf(" ***  TEST-06 OK ***\n");
   }

   printf("***  TEST ENDED ***\n");
}