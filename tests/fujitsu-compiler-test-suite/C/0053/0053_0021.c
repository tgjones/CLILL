#include<stdio.h>
int main()
{
  printf("**********  TEST START **********\n");
  {
    int a = 1;

    &a;
    if (a == 1)
        printf("*****  - 01 ***** O K *****\n");
      else
        printf("*****  - 01 ***** N G *****\n");
  }
  {
    static int a[2] = { 1,2 },b = 1,*c = &b,d;

    d = a[*c];
    if (d == 2)
        printf("*****  - 02 ***** O K *****\n");
      else
        printf("*****  - 02 ***** N G *****\n");
  }
  {
    int a = 1;

    a,++a;
    if (a == 2)
        printf("*****  - 03 ***** O K *****\n");
      else
        printf("*****  - 03 ***** N G *****\n");
  }
  {
    int a,b = 2;

    a = ++b;
    if (a == 3)
        printf("*****  - 04 ***** O K *****\n");
      else
        printf("*****  - 04 ***** N G *****\n");
  }
  {
    int a = 1,b = 2,*c = &b,d = 3,e;

    e = a ? *c : d;
    if (e == 2)
        printf("*****  - 05 ***** O K *****\n");
      else
        printf("*****  - 05 ***** N G *****\n");
  }
  {
    int a = 0,b = 2,c = 3,*d = &c,e;

    e = a ? b : *d;
    if (e == 3)
        printf("*****  - 06 ***** O K *****\n");
      else
        printf("*****  - 06 ***** N G *****\n");
  }
  {
    int a,b = 0,c = 1,*d = &c;

    a = b || *d;
    if (a == 1)
        printf("*****  - 07 ***** O K *****\n");
      else
        printf("*****  - 07 ***** N G *****\n");
  }
  {
    int a,b = 1,c = 1,*d = &c;

    a = b && *d;
    if (a == 1)
        printf("*****  - 08 ***** O K *****\n");
      else
        printf("*****  - 08 ***** N G *****\n");
  }
  {
    int a,b = 1,c = 2,*d = &c;

    a = b | *d;
    if (a == 3)
        printf("*****  - 09 ***** O K *****\n");
      else
        printf("*****  - 09 ***** N G *****\n");
  }
  {
    int a,b = 2,c = 1,*d = &c;

    a = b ^ *d;
    if (a == 3)
        printf("*****  - 10 ***** O K *****\n");
      else
        printf("*****  - 10 ***** N G *****\n");
  }
  {
    int a,b = 3,c = 2,*d = &c;

    a = b & *d;
    if (a == 2)
        printf("*****  - 11 ***** O K *****\n");
      else
        printf("*****  - 11 ***** N G *****\n");
  }
  {
    int a,b = 1,c = 2,*d = &c;

    a = b != *d;
    if (a != 0)
        printf("*****  - 12 ***** O K *****\n");
      else
        printf("*****  - 12 ***** N G *****\n");
  }
  {
    int a,b = 1,c = 2,*d = &c;

    a = b <= *d;
    if (a != 0)
        printf("*****  - 13 ***** O K *****\n");
      else
        printf("*****  - 13 ***** N G *****\n");
  }
  {
    int a,b = 2,c = 1,*d = &c;

    a = b << *d;
    if (a == 4)
        printf("*****  - 14 ***** O K *****\n");
      else
        printf("*****  - 14 ***** N G *****\n");
  }
  {
    int a,b = 3,c = 2,*d = &c;

    a = b + *d;
    if (a == 5)
        printf("*****  - 15 ***** O K *****\n");
      else
        printf("*****  - 15 ***** N G *****\n");
  }
  {
    int a,b = 3,c = 2,*d = &c;

    a = b * *d;
    if (a == 6)
        printf("*****  - 16 ***** O K *****\n");
      else
        printf("*****  - 16 ***** N G *****\n");
  }
  {
    int a,b = 1,*c = &b;

    a = - *c;
    if (a == -1)
        printf("*****  - 17 ***** O K *****\n");
      else
        printf("*****  - 17 ***** N G *****\n");
  }
  {
    int a,b = 2;

    a = b++;
    if (a == 2)
        printf("*****  - 18 ***** O K *****\n");
      else
        printf("*****  - 18 ***** N G *****\n");
  }
  {
    int a;
    static struct { int a; } st = { 2 };

    a = st.a;
    if (a == 2)
        printf("*****  - 19 ***** O K *****\n");
      else
        printf("*****  - 19 ***** N G *****\n");
  }
  printf("**********  TEST  END  **********\n");
}