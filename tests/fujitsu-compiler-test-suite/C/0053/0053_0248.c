#include<stdio.h>
   struct tag2 {int a; int b;}g();
int main()
  {
   printf("*** \n");
{
   int i=1;
   int a=1;
   int *pq;
   int **pp,**qq;
   pq=&a;
   pp=&pq;
   qq=&pq;
   i=pp < qq;
   if(i==0)
     printf("*** \n");
   else
     printf("*** \n");
}
{
   int i=1;
   struct tag {int a; int b;}st,*pq=&st,**qq=&pq,**pp=&pq;
   i=pp < qq;
   if(i==0)
     printf("*** \n");
   else
     printf("*** \n");
}
{
   int i=1;
   static int arr[2]={1,2},(*p)[2],(*q)[2];
   p=&arr; q=&arr;
   i=p < q;
   if(i==0)
     printf("*** \n");
   else
     printf("*** \n");
}
{
   int i=1;
   static struct tag {
                int a;
                int b;
           } arr[2]={1,2},(*p)[2],(*q)[2];
   p=&arr; q=&arr;
   i=p < q;
   if(i==0)
     printf("*** \n");
   else
     printf("*** \n");
}
{
   int i=1;
   int f();
   int (*p)(),(*q)();
   p=f;
   q=f;
     printf("*** \n");
}
{
   int i=1;
   struct tag2 (*p)();
   struct tag2(*q)();
   p=g;
   q=g;
     printf("*** \n");
}
{
   int i=1,a=1;
   int *p,*q;
   p=&a;
   q=&a;
   i=p < q;
   if(i==0)
     printf("*** \n");
   else
     printf("*** \n");
}
{
   int i=1,a=1;
   struct tag {int a;int b;}st,*p=&st,*q=&st;
   i=p < q;
   if(i==0)
     printf("*** \n");
   else
     printf("*** \n");
}
printf("*** \n");
  }
int f()
{}
struct tag2 g()
{}