#include <stdio.h>
char sub(char c)
{
  if(c==5)
    return 1;
  else
    return 0;
}
int main()
{
  char a=1,b=0,c=1; 
  int i;
  for(i=0;i<10;i++){
    if(sub((int)c)) break;
    else if(a==0 && b==1)
      c+=1;
    else
      a=0;b=1;
  }
  if(c==5)
    printf("ok\n");
  else
    printf("ng c=%d\n",c);
}
