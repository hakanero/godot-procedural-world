class vec2:
    def __init__(self,x,y):
        self.x = x
        self.y = y
    def __str__(self):
        return f"{self.x},{self.y}"
    def distance_sqr_to(self,to):
        return (to.x-self.x)**2 + (to.y-self.y)**2

arr = []
dst = []
cd = 1000

for i in range(cd):
    for j in range(i+1):
        arr.append(vec2(i-j,j))
        arr.append(vec2(j-i,j))
        arr.append(vec2(i-j,-j))
        arr.append(vec2(j-i,-j))

origin = vec2(0,0)

for i in arr:
    dst.append(origin.distance_sqr_to(i))

print(max(dst))
