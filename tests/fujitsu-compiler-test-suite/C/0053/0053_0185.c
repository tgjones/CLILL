#include<stdio.h>
void f3(volatile const unsigned long int,int);
void f2(volatile const unsigned int,int);
void f1(volatile const int,int);
void f0(volatile const int ,int);
int main()
{
  f3(1ul,2);
  f2(1u,2);
  f1(1,2);
  f0(1,2);
}

void f3(volatile const unsigned long int a,int b)
{
  if (a == 1ul)
      printf("***** \n");
    else
      printf("***** \n");

  if (b == 2)
      printf("***** \n");
    else
      printf("***** \n");
}

void f2(volatile const unsigned int a,int b)
{
  if (a == 1u)
      printf("***** \n");
    else
      printf("***** \n");

  if (b == 2)
      printf("***** \n");
    else
      printf("***** \n");
}

void f1(volatile const int a,int b)
{
  if (a == 1)
      printf("***** \n");
    else
      printf("***** \n");

  if (b == 2)
      printf("***** \n");
    else
      printf("***** \n");
}

void f0(volatile const  int a,int b)
{
  if (a == 1)
      printf("***** \n");
    else
      printf("***** \n");

  if (b == 2)
      printf("***** \n");
    else
      printf("***** \n");
}