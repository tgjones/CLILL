#include <stdio.h>
#include <string.h>
#include <math.h>
#include <stdlib.h>

int main()
{
  long int i1a[5][5],i1b[5][5],i1c[5][5],i1d[5][5],i1e[5][5];
  long int i2a[5][5],i2b[5][5],i2c[5][5],i2d[5][5],i2e[5][5];
  long int i3a[5][5],i3b[5][5],i3c[5][5],i3d[5][5],i3e[5][5];
  long int i4a[5][5],i4b[5][5],i4c[5][5],i4d[5][5],i4e[5][5];
  double   r1a[5][5],r1b[5][5],r1c[5][5],r1d[5][5],r1e[5][5];
  double   r2a[5][5],r2b[5][5],r2c[5][5],r2d[5][5],r2e[5][5];
  double   r3a[5][5],r3b[5][5],r3c[5][5],r3d[5][5],r3e[5][5];
  double   r4a[5][5],r4b[5][5],r4c[5][5],r4d[5][5],r4e[5][5];
  long int i,j;
  puts(" test start");
  for(j=0;j<5;j++)
  {
    for(i=0;i<5;i++)
    {
      if(i==0 || i==2 || i==4)
      {
        i1a[i][j] = i1b[i][j] = i1c[i][j] = i1d[i][j] = i1e[i][j] = 1;
        i2a[i][j] = i2b[i][j] = i2c[i][j] = i2d[i][j] = i2e[i][j] = 2;
        i3a[i][j] = i3b[i][j] = i3c[i][j] = i3d[i][j] = i3e[i][j] = 3;
        i4a[i][j] = i4b[i][j] = i4c[i][j] = i4d[i][j] = i4e[i][j] = 4;
        r1a[i][j] = r1b[i][j] = r1c[i][j] = r1d[i][j] = r1e[i][j] = 1.0;
        r2a[i][j] = r2b[i][j] = r2c[i][j] = r2d[i][j] = r2e[i][j] = 2.0;
        r3a[i][j] = r3b[i][j] = r3c[i][j] = r3d[i][j] = r3e[i][j] = 3.0;
        r4a[i][j] = r4b[i][j] = r4c[i][j] = r4d[i][j] = r4e[i][j] = 4.0;
      }
      else
      {
        i1a[i][j] = i1b[i][j] = i1c[i][j] = i1d[i][j] = i1e[i][j] = 4;
        i2a[i][j] = i2b[i][j] = i2c[i][j] = i2d[i][j] = i2e[i][j] = 3;
        i3a[i][j] = i3b[i][j] = i3c[i][j] = i3d[i][j] = i3e[i][j] = 2;
        i4a[i][j] = i4b[i][j] = i4c[i][j] = i4d[i][j] = i4e[i][j] = 1;
        r1a[i][j] = r1b[i][j] = r1c[i][j] = r1d[i][j] = r1e[i][j] = 4.0;
        r2a[i][j] = r2b[i][j] = r2c[i][j] = r2d[i][j] = r2e[i][j] = 3.0;
        r3a[i][j] = r3b[i][j] = r3c[i][j] = r3d[i][j] = r3e[i][j] = 2.0;
        r4a[i][j] = r4b[i][j] = r4c[i][j] = r4d[i][j] = r4e[i][j] = 1.0;
      }
     }
   }
   
   for(i=0;i<5;i++)
   {
     for(j=0;j<5;j++)
     {
       i1a[i][j] = 1 + i1b[i][j];
       i1b[i][j] = labs(i1a[1][j] + i1c[i][j]);
       i1d[i][j] = i1e[i][j] + i;
       r1a[i][j] = 1 + r1b[i][j];
       r1b[i][j] = sqrt(r1a[i][j] + r1c[i][j]);
       r1d[i][j] = r1e[i][j] + i;
     }
     r1a[1][1] = 10;
   }
   puts("item 1");
   for(j=0;j<5;j++)
   {
     for(i=0;i<5;i++)
     {
        printf(" %ld %ld %ld ",i1a[i][j],i1b[i][j],i1c[i][j]);
        printf(" %ld %ld \n  ",i1d[i][j],i1e[i][j]);
        printf(" %g %g %g ",r1a[i][j],r1b[i][j],r1c[i][j]);
        printf(" %g %g \n  ",r1d[i][j],r1e[i][j]);
     }
   }
   
   for(i=0;i<5;i++)
   {
     for(j=0;j<4;j++)
     {
       i2a[i][j] = 1 + i2b[i][j];
       i2b[i][j] = labs(i2a[i][j] + i2c[i][j]);
       i2d[i][j] = i2e[i][j] + i;
       r2a[i][j] = 1 + r2b[i][j+1];
       r2b[i][j] = sqrt(r2a[i][j] + r2c[i][j]);
       r2d[i][j] = r2e[i][j] + i;
     }
   }
   puts("item 2");
   for(j=0;j<5;j++)
   {
     for(i=0;i<5;i++)
     {
        printf(" %ld %ld %ld ",i2a[i][j],i2b[i][j],i2c[i][j]);
        printf(" %ld %ld \n  ",i2d[i][j],i2e[i][j]);
        printf(" %g %g %g ",r2a[i][j],r2b[i][j],r2c[i][j]);
        printf(" %g %g \n  ",r2d[i][j],r2e[i][j]);
     }
   }
   
   for(j=0;j<5;j++)
   {
     for(i=0;i<5;i++)
     {
       i3a[2][2] = i3b[2][2] + i3c[i][j];
       i3b[2][2] = labs(i3a[2][2]);
       i3c[i][j] = i3d[i][j] + i3e[i][j];
       r3a[i][j] = sqrt(r3b[i][j] + r3c[i][j]);
       r3d[i][j] = r3d[i][j] - r3a[i][j];
     }
       r3d[2][2] = 11;
   }
   puts("item 3");
   for(j=0;j<5;j++)
   {
     for(i=0;i<5;i++)
     {
        printf(" %ld %ld %ld ",i3a[i][j],i3b[i][j],i3c[i][j]);
        printf(" %ld %ld \n  ",i3d[i][j],i3e[i][j]);
        printf(" %g %g %g ",r3a[i][j],r3b[i][j],r3c[i][j]);
        printf(" %g %g \n  ",r3d[i][j],r3e[i][j]);
       }
     }
   
   j = 3;
   for(i=0;i<4;i++)
   {
     i4a[i][2] = i4a[i][j] + i;
     i4b[i][j] = abs(i4a[i][j] + i4c[i][j]);
     i4c[i][j] = i4d[i+1][j+1] * i4e[i][j];
   }
   puts("item 4");
   for(j=0;j<5;j++)
   {
     for(i=0;i<5;i++)
     {
        printf(" %ld %ld %ld ",i4a[i][j],i4b[i][j],i4c[i][j]);
        printf(" %ld %ld \n  ",i4d[i][j],i4e[i][j]);
        printf(" %g %g %g ",r4a[i][j],r4b[i][j],r4c[i][j]);
        printf(" %g %g \n  ",r4d[i][j],r4e[i][j]);
     }
   }
   
 puts(" test ended");
exit (0);
}