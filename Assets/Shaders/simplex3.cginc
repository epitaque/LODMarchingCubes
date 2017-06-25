#ifndef NOISE_SIMPLEX_FUNC
#define NOISE_SIMPLEX_FUNC

int simplexSeed;
 
int hash(int a)
{
    a = (a ^ 61) ^ (a >> 16);
    a = a + (a << 3);
    a = a ^ (a >> 4);
    a = a * simplexSeed;
    a = a ^ (a >> 15);
    return a;
}
 
/*
** 1D
*/
 
void grad1( int hash,  out float gx )
{
    int h = hash & 15;
    gx = 1.0f + (h & 7);
    if (h&8) gx = -gx;
}
 
float noise( float x )
{
  int i0 = floor(x);
  int i1 = i0 + 1;
  float x0 = x - i0;
  float x1 = x0 - 1.0f;
 
  float gx0, gx1;
  float n0, n1;
  float t20, t40, t21, t41;
 
  float x20 = x0 * x0;
  float t0 = 1.0f - x20;
  t20 = t0 * t0;
  t40 = t20 * t20;
  grad1(hash(i0 & 0xff), gx0);
  n0 = t40 * gx0 * x0;
 
  float x21 = x1*x1;
  float t1 = 1.0f - x21;
  t21 = t1 * t1;
  t41 = t21 * t21;
  grad1(hash(i1 & 0xff), gx1);
  n1 = t41 * gx1 * x1;
 
  return 0.25f * (n0 + n1);
}
 
float noise (float x, out float dnoise_dx)
{
  int i0 = floor(x);
  int i1 = i0 + 1;
  float x0 = x - i0;
  float x1 = x0 - 1.0f;
 
  float gx0, gx1;
  float n0, n1;
  float t20, t40, t21, t41;
 
  float x20 = x0 * x0;
  float t0 = 1.0f - x20;
  t20 = t0 * t0;
  t40 = t20 * t20;
  grad1(hash(i0 & 0xff), gx0);
  n0 = t40 * gx0 * x0;
 
  float x21 = x1*x1;
  float t1 = 1.0f - x21;
  t21 = t1 * t1;
  t41 = t21 * t21;
  grad1(hash(i1 & 0xff), gx1);
  n1 = t41 * gx1 * x1;
 
  dnoise_dx = t20 * t0 * gx0 * x20;
  dnoise_dx += t21 * t1 * gx1 * x21;
  dnoise_dx *= -8.0f;
  dnoise_dx += t40 * gx0 + t41 * gx1;
  dnoise_dx *= 0.1;
  return 0.25f * (n0 + n1);
}
 
/*
** 2D
*/
 
static    float2 grad2lut[8] =
   {
     { -1.0f, -1.0f }, { 1.0f, 0.0f } , { -1.0f, 0.0f } , { 1.0f, 1.0f } ,
     { -1.0f, 1.0f } , { 0.0f, -1.0f } , { 0.0f, 1.0f } , { 1.0f, -1.0f }
   };
 
float2 grad2( int hash)
{
    return grad2lut[hash & 7];
}
 
float noise( float2 input)
  {
    float n0, n1, n2;
    float2 g0, g1, g2;
 
    float s = ( input.x + input.y ) * 0.366025403f;
    float2 a = input + s;
    int2 ij = floor( a );
 
    float t = ( float ) ( ij.x + ij.y ) * 0.211324865f;
    float2 b = ij - t;
    float2 c = input - b;
 
    int2 ij1 = c.x > c.y ? float2(1,0) : float2(0,1);
 
   float2 c1 = c - ij1 + 0.211324865f;
   float2 c2 = c - 1.0f + 2.0f * 0.211324865f;
 
    int ii = ij.x & 0xff;
    int jj = ij.y & 0xff;
 
    float t0 = 0.5f - c.x * c.x - c.y * c.y;
    float t20, t40;
    if( t0 < 0.0f ) t40 = t20 = t0 = n0 = g0.x = g0.y = 0.0f;
    else
    {
      g0 = grad2( hash(ii + hash(jj)));
      t20 = t0 * t0;
      t40 = t20 * t20;
      n0 = t40 * ( g0.x * c.x + g0.y * c.y );
    }
 
    float t1 = 0.5f - c1.x * c1.x - c1.y * c1.y;
    float t21, t41;
    if( t1 < 0.0f ) t21 = t41 = t1 = n1 = g1.x = g1.y = 0.0f;
    else
    {
      g1 = grad2( hash(ii + ij1.x + hash(jj + ij1.y)));
      t21 = t1 * t1;
      t41 = t21 * t21;
      n1 = t41 * ( g1.x * c1.x + g1.y * c1.y );
    }
 
    float t2 = 0.5f - c2.x * c2.x - c2.y * c2.y;
    float t22, t42;
    if( t2 < 0.0f ) t42 = t22 = t2 = n2 = g2.x = g2.y = 0.0f;
    else
    {
      g2 = grad2( hash(ii + 1 + hash(jj + 1)));
      t22 = t2 * t2;
      t42 = t22 * t22;
      n2 = t42 * ( g2.x * c2.x + g2.y * c2.y );
    }
 
    float noise = 40.0f * ( n0 + n1 + n2 );
    return noise;
  }
 
float noise( float2 input, out float2 derivative)
  {
    float n0, n1, n2;
    float2 g0, g1, g2;
 
    float s = ( input.x + input.y ) * 0.366025403f;
    float2 a = input + s;
    int2 ij = floor( a );
 
    float t = ( float ) ( ij.x + ij.y ) * 0.211324865f;
    float2 b = ij - t;
    float2 c = input - b;
 
    int2 ij1 = c.x > c.y ? float2(1,0) : float2(0,1);
 
   float2 c1 = c - ij1 + 0.211324865f;
   float2 c2 = c - 1.0f + 2.0f * 0.211324865f;
 
    int ii = ij.x & 0xff;
    int jj = ij.y & 0xff;
 
    float t0 = 0.5f - c.x * c.x - c.y * c.y;
    float t20, t40;
    if( t0 < 0.0f ) t40 = t20 = t0 = n0 = g0.x = g0.y = 0.0f;
    else
    {
      g0 = grad2( hash(ii + hash(jj)));
      t20 = t0 * t0;
      t40 = t20 * t20;
      n0 = t40 * ( g0.x * c.x + g0.y * c.y );
    }
 
    float t1 = 0.5f - c1.x * c1.x - c1.y * c1.y;
    float t21, t41;
    if( t1 < 0.0f ) t21 = t41 = t1 = n1 = g1.x = g1.y = 0.0f;
    else
    {
      g1 = grad2( hash(ii + ij1.x + hash(jj + ij1.y)));
      t21 = t1 * t1;
      t41 = t21 * t21;
      n1 = t41 * ( g1.x * c1.x + g1.y * c1.y );
    }
 
    float t2 = 0.5f - c2.x * c2.x - c2.y * c2.y;
    float t22, t42;
    if( t2 < 0.0f ) t42 = t22 = t2 = n2 = g2.x = g2.y = 0.0f;
    else
    {
      g2 = grad2( hash(ii + 1 + hash(jj + 1)));
      t22 = t2 * t2;
      t42 = t22 * t22;
      n2 = t42 * ( g2.x * c2.x + g2.y * c2.y );
    }
 
    float noise = 40.0f * ( n0 + n1 + n2 );
 
    float temp0 = t20 * t0 * ( g0.x * c.x + g0.y * c.y );
    float temp1 = t21 * t1 * ( g1.x * c1.x + g1.y * c1.y );
    float temp2 = t22 * t2 * ( g2.x * c2.x + g2.y * c2.y );
    derivative = ((temp0 * c + temp1 * c1 + temp2 * c2) * -8 + (t40 * g0 + t41 * g1 + t42 * g2)) * 40;
     
    return noise;
  }
 
  /*
  ** 3D
  */                    
 
   static float3 grad3lut[16] =
   {
     { 1.0f, 0.0f, 1.0f }, { 0.0f, 1.0f, 1.0f },
     { -1.0f, 0.0f, 1.0f }, { 0.0f, -1.0f, 1.0f },
     { 1.0f, 0.0f, -1.0f }, { 0.0f, 1.0f, -1.0f },
     { -1.0f, 0.0f, -1.0f }, { 0.0f, -1.0f, -1.0f },
     { 1.0f, -1.0f, 0.0f }, { 1.0f, 1.0f, 0.0f },
     { -1.0f, 1.0f, 0.0f }, { -1.0f, -1.0f, 0.0f },
     { 1.0f, 0.0f, 1.0f }, { -1.0f, 0.0f, 1.0f },
     { 0.0f, 1.0f, -1.0f }, { 0.0f, -1.0f, -1.0f }
   };
 
   float3 grad3(int hash)
   {
      return grad3lut[hash & 15];
   }
     
  float simplexNoise(float3 input)
  {
    float n0, n1, n2, n3;
    float noise;
    float3 g0, g1, g2, g3;
 
    float s = (input.x + input.y + input.z) * 0.333333333;
    float3 a = input + s;
    int3 ijk = floor(a);
 
    float t = (float)(ijk.x + ijk.y + ijk.z) * 0.166666667;
    float3 b = ijk - t;
   float3 c = input - b;
 
   int3 ijk1;
   int3 ijk2;
   
    if(c.x >= c.y) {
      if(c.y >= c.z)
        { ijk1 = int3(1, 0, 0); ijk2 = int3(1,1,0); }
        else if(c.x >= c.z) { ijk1 = int3(1, 0, 0); ijk2 = int3(1,0,1); }
        else { ijk1 = int3(0, 0, 1); ijk2 = int3(1,0,1); }
      }
    else {
      if(c.y < c.z) { ijk1 = int3(0, 0, 1); ijk2 = int3(0,1,1); }
      else if(c.x < c.z) { ijk1 = int3(0, 1, 0); ijk2 = int3(0,1,1); }
      else { ijk1 = int3(0, 1, 0); ijk2 = int3(1,1,0); }
    }
 
    float3 c1 = c - ijk1 + 0.166666667;
   float3 c2 = c - ijk2 + 2.0f * 0.166666667;
   float3 c3 = c - 1.0f + 3.0f * 0.166666667;
 
    int ii = ijk.x & 0xff;
    int jj = ijk.y & 0xff;
    int kk = ijk.z & 0xff;
 
    float t0 = 0.6f - c.x * c.x - c.y * c.y - c.z * c.z;
    float t20, t40;
    if(t0 < 0.0f) n0 = t0 = t20 = t40 = g0.x = g0.y = g0.z = 0.0f;
    else {
      g0 = grad3( hash(ii + hash(jj + hash(kk))));
      t20 = t0 * t0;
      t40 = t20 * t20;
      n0 = t40 * ( g0.x * c.x + g0.y * c.y + g0.z * c.z );
    }
 
    float t1 = 0.6f - c1.x * c1.x -  c1.y * c1.y - c1.z * c1.z;
    float t21, t41;
    if(t1 < 0.0f) n1 = t1 = t21 = t41 = g1.x = g1.y = g1.z = 0.0f;
    else {
      g1 = grad3( hash(ii + ijk1.x + hash(jj + ijk1.y + hash(kk + ijk1.z))));
      t21 = t1 * t1;
      t41 = t21 * t21;
      n1 = t41 * ( g1.x * c1.x + g1.y * c1.y + g1.z * c1.z );
    }
 
    float t2 = 0.6f - c2.x * c2.x - c2.y * c2.y - c2.z * c2.z;
    float t22, t42;
    if(t2 < 0.0f) n2 = t2 = t22 = t42 = g2.x = g2.y = g2.z = 0.0f;
    else {
      g2 = grad3( hash(ii + ijk2.x + hash(jj + ijk2.y + hash(kk + ijk2.z))));
      t22 = t2 * t2;
      t42 = t22 * t22;
      n2 = t42 * ( g2.x * c2.x + g2.y * c2.y + g2.z * c2.z );
    }
 
    float t3 = 0.6f - c3.x * c3.x - c3.y * c3.y - c3.z * c3.z;
    float t23, t43;
    if(t3 < 0.0f) n3 = t3 = t23 = t43 = g3.x = g3.y = g3.z = 0.0f;
    else {
      g3 = grad3( hash(ii + 1 + hash(jj + 1 + hash(kk + 1))));
      t23 = t3 * t3;
      t43 = t23 * t23;
      n3 = t43 * ( g3.x * c3.x + g3.y * c3.y + g3.z * c3.z );
    }
 
    noise = 20.0f * (n0 + n1 + n2 + n3);
    return noise;
}
   
  float simplexNoise( float3 input, out float3 derivative)
  {
    float n0, n1, n2, n3;
    float noise;
    float3 g0, g1, g2, g3;
 
    float s = (input.x + input.y + input.z) * 0.333333333;
    float3 a = input + s;
    int3 ijk = floor(a);
 
    float t = (float)(ijk.x + ijk.y + ijk.z) * 0.166666667;
    float3 b = ijk - t;
   float3 c = input - b;
 
   int3 ijk1;
   int3 ijk2;
   
    if(c.x >= c.y) {
      if(c.y >= c.z)
        { ijk1 = int3(1, 0, 0); ijk2 = int3(1,1,0); }
        else if(c.x >= c.z) { ijk1 = int3(1, 0, 0); ijk2 = int3(1,0,1); }
        else { ijk1 = int3(0, 0, 1); ijk2 = int3(1,0,1); }
      }
    else {
      if(c.y < c.z) { ijk1 = int3(0, 0, 1); ijk2 = int3(0,1,1); }
      else if(c.x < c.z) { ijk1 = int3(0, 1, 0); ijk2 = int3(0,1,1); }
      else { ijk1 = int3(0, 1, 0); ijk2 = int3(1,1,0); }
    }
 
    float3 c1 = c - ijk1 + 0.166666667;
   float3 c2 = c - ijk2 + 2.0f * 0.166666667;
   float3 c3 = c - 1.0f + 3.0f * 0.166666667;
 
    int ii = ijk.x & 0xff;
    int jj = ijk.y & 0xff;
    int kk = ijk.z & 0xff;
 
    float t0 = 0.6f - c.x * c.x - c.y * c.y - c.z * c.z;
    float t20, t40;
    if(t0 < 0.0f) n0 = t0 = t20 = t40 = g0.x = g0.y = g0.z = 0.0f;
    else {
      g0 = grad3( hash(ii + hash(jj + hash(kk))));
      t20 = t0 * t0;
      t40 = t20 * t20;
      n0 = t40 * ( g0.x * c.x + g0.y * c.y + g0.z * c.z );
    }
 
    float t1 = 0.6f - c1.x * c1.x -  c1.y * c1.y - c1.z * c1.z;
    float t21, t41;
    if(t1 < 0.0f) n1 = t1 = t21 = t41 = g1.x = g1.y = g1.z = 0.0f;
    else {
      g1 = grad3( hash(ii + ijk1.x + hash(jj + ijk1.y + hash(kk + ijk1.z))));
      t21 = t1 * t1;
      t41 = t21 * t21;
      n1 = t41 * ( g1.x * c1.x + g1.y * c1.y + g1.z * c1.z );
    }
 
    float t2 = 0.6f - c2.x * c2.x - c2.y * c2.y - c2.z * c2.z;
    float t22, t42;
    if(t2 < 0.0f) n2 = t2 = t22 = t42 = g2.x = g2.y = g2.z = 0.0f;
    else {
      g2 = grad3( hash(ii + ijk2.x + hash(jj + ijk2.y + hash(kk + ijk2.z))));
      t22 = t2 * t2;
      t42 = t22 * t22;
      n2 = t42 * ( g2.x * c2.x + g2.y * c2.y + g2.z * c2.z );
    }
 
    float t3 = 0.6f - c3.x * c3.x - c3.y * c3.y - c3.z * c3.z;
    float t23, t43;
    if(t3 < 0.0f) n3 = t3 = t23 = t43 = g3.x = g3.y = g3.z = 0.0f;
    else {
      g3 = grad3( hash(ii + 1 + hash(jj + 1 + hash(kk + 1))));
      t23 = t3 * t3;
      t43 = t23 * t23;
      n3 = t43 * ( g3.x * c3.x + g3.y * c3.y + g3.z * c3.z );
    }
 
    noise = 20.0f * (n0 + n1 + n2 + n3);
 
    float temp0 = t20 * t0 * ( g0.x * c.x + g0.y * c.y + g0.z * c.z );
    derivative = temp0 * c;
    float temp1 = t21 * t1 * ( g1.x * c1.x + g1.y * c1.y + g1.z * c1.z );
    derivative += temp1 * c1;
    float temp2 = t22 * t2 * ( g2.x * c2.x + g2.y * c2.y + g2.z * c2.z );
    derivative += temp2 * c2;
    float temp3 = t23 * t3 * ( g3.x * c3.x + g3.y * c3.y + g3.z * c3.z );
    derivative += temp3 * c3;
    derivative *= -8.0f;
    derivative += t40 * g0 + t41 * g1 + t42 * g2 + t43 * g3;
    derivative *= 28.0f;
 
    return noise;
}
#endif
