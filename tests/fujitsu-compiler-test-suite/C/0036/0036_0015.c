#include <stdio.h>
int sub1 (int ii4b[10][10], float rr4b[10][10], double rr8b[10][10], int n, int m);
int main()
{
  int   i4a[10][10],i4b[10][10],i4c[10][10],i4d;
  float r4a[10][10],r4b[10][10],r4c[10][10],r4d;
  double r8a[10][10],r8b[10][10],r8c[10][10],r8d;
  int  i,j;
  for(j=0;j<10;j++)
  for(i=0;i<10;i++)
    {
      i4a[i][j] = 0;
      i4b[i][j] = i;
      i4c[i][j] = i+2;
      r4a[i][j] = 0;
      r4b[i][j] = i;
      r4c[i][j] = i+2;
      r8a[i][j] = 0;
      r8b[i][j] = i;
      r8c[i][j] = i+2;
    }

  i4d = 0;
  for(j=0;j<10;j++)
  for(i=0;i<10;i++)
    {
      i4d = i4d + i4b[i][j]-i4c[i][j];
    }
    printf("%d \n",i4d);

  r4d = 0;
  for(i=0;i<10;i++)
  for(j=0;j<10;j++)
    {
      r4d = r4d + r4b[i][j]+r4c[i][j];
    }
    printf("%f \n",r4d);

  r8d = 0;
  for(i=0;i<10;i++)
  for(j=0;j<10;j++)
    {
      r8d = r8d + r8b[i][j]*r8c[i][j];
    }

    printf("%f \n",r8d);

    sub1(i4b,r4b,r8b,0,10);
}

int 
sub1 (int ii4b[10][10], float rr4b[10][10], double rr8b[10][10], int n, int m)
     
{
  int i4e=0,i,j;
  float  r4e=0;
  double r8e=0;
  
  for(i=n;i<m;i++)
  for(j=n;j<m;j++)
    {
      i4e = i4e + ii4b[i][j];
    }
    printf("%d \n",i4e);

  for(j=n;j<m;j++)
  for(i=n;i<m;i++)
    {
      r4e = r4e - rr4b[i][j];
    }
    printf("%f \n",r4e);

  for(j=n;j<m;j++)
  for(i=n;i<m;i++)
    {
      r8e = r8e + rr8b[i][j]+2;
    }

    printf("%f \n",r8e);
  
}
