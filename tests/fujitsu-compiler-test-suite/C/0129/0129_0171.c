#include <stdlib.h>
#include <math.h>
#include  <stdio.h>
#define  LEN 11

static char  *ch_table[]={
	"0123456789","1234567890","2345678901","3456789012",
	"4567890123","5678901234","6789012345","7890123456",
	"8901234567","9012345678"
	};

char	buf[LEN];
char    *bufp = &buf[0];
long long   Entrycount=0;

struct  xx{
  long long num;
  char  ch[LEN];
};
int main()
{
  struct  xx box[10],g(),work;
  long long func(),i=0,f,constret(),j;

  f=0;
  for (j=0;j<10;j++){
    i=9LL;
    work=g(box[j],j) ;
    f+=func(&work,i,j) ;
  }
  if (f==constret(90LL)&&Entrycount==constret(55LL))
    printf(" OK\n");
  else
    printf(" NG  f=%d (90) Entrycount=%d (55)\n",f,Entrycount);
exit (0);
}
struct xx g(in,j)
struct xx in;
long	long  j;
{
  in.num = j;
  return in;
}
long long func(str,i,j)
struct xx *str;
long long i,j;
{
  void Strcpy();
  char *getch();
  struct xx *p;
  Entrycount++;
  p=str;
  if (i==j) {
    Strcpy(p->ch,getch(i,i,i,i,i,i,i,i,i,i));
    return p->ch[5] - '0'+p->num;
  }
  else{
    return func(p++,i-1,j);
  }
}
void Strcpy(a,b)
char *a,*b;
{
  long	long i;
  for (i=0;*b != '\0' && *b != '\n';)
    a[i++]= *b++;
  return;
}
char *getch(a,b,c,d,e,f,g,h,i,j)
long long a,b,c,d,e,f,g,h,i,j;
{
  long long max=a-1;

  if (b>max) max=b;
  if (!(max>c)) max=c;
  if (d>=max) max=d;
  if (!(max>=0)) max=e;
  if (max-f>0) max=f;
  if (max+g > g+h) max=h;
  if (max - i >= j - i) max=j;
  Strcpy(bufp,&ch_table[max][max-max]);
  return bufp;
}
long long constret(n)
  long long n;
{
  long long ret;

  if (n==0) return 0;
  ret=n*(n/n)/(n+1/n+1);
  if (ret==0) ret=n;
  return (ret*(ret/ret)==ret) ? ret :n;
}