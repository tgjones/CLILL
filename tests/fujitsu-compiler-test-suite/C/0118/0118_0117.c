
#include <stdio.h>

#define loop_num 10000
#define Correct_answer 10970

void sub(double *a, double *b ,int n)
{
  int i;
  for (i=0; i<n-300; i=i+100) {
    a[i] = a[i] + b[i+200] + b[i] + b[i+1] + b[i+300] + b[i+310];
  }
  return;
}

int main()
{
  double a[loop_num], b[loop_num];
  int i,result;

  for (i=0; i<loop_num; i++) {
    a[i] = 1.0;
    b[i] = 2.0;
  }

  sub(a,b,loop_num);

  result = 0.0;
  for (i=0; i<loop_num; i++) {
    result += a[i];
  }
  
  if (result == Correct_answer) {
    printf("ok\n");
  } else {
    printf("ng sub %d\n",result);
  }

  return 0;
}