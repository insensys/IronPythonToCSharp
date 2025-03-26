import math

pushinka=0.001

def dist(a,b):
	c = math.sqrt((b[0] - a[0])*(b[0] - a[0])+(b[1] - a[1])*(b[1] - a[1])+(b[2] - a[2])*(b[2] - a[2]))
	return c


import clr
from AutoCAD import *

from System import *
from System.Diagnostics import *
from System.Runtime.InteropServices import *

def get_acad_obj():
	aCAD =  Process.GetProcessesByName("acad")
	if len(aCAD) == 0:
		print "автокад не запущен"
		return None
	else:
		return Marshal.GetActiveObject("AutoCAD.Application")

def interpol(n1,n2,kaisy,kancha):
	return  (n2-n1)*kaisy/kancha+n1


standalone = False
# standalone = True

def make_array(elem_type,*args):
	from System import Array,Type
	result = Array.CreateInstance(Type.GetType(elem_type),len(args))
	for i,arg in enumerate(args):
		result.SetValue(arg,i)
	return result

def make_point(*args):
	pnt = make_array("System.Double",*[0,0,0])
	for i in range(min(3,len(args))):
		pnt.SetValue(args[i],i)
	return pnt
	
if standalone:
	acadApp = get_acad_obj()
	doc = acadApp.ActiveDocument
else:
	import Autodesk.AutoCAD.ApplicationServices as ap
	doc = ap.Application.DocumentManager.MdiActiveDocument.AcadDocument
	acadApp = doc.Application

		
doc.ActiveSpace = AcActiveSpace.acModelSpace 




pnt1 = Array.CreateInstance(Type.GetType("System.Double"),3)
for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nБашындагы чекитти танда: ")):
	pnt1.SetValue(coord,i)

pnt2 = Array.CreateInstance(Type.GetType("System.Double"),3)
for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nАягындагы  чекитти танда: ")):
	pnt2.SetValue(coord,i)


pntA = Array.CreateInstance(Type.GetType("System.Double"),3)
for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nАркадагы чекитти танда: ")):
	pntA.SetValue(coord,i)

kancha=doc.Utility.GetInteger(Prompt = "\nарасын канчага майдалайлы?: ")


pnt3 = Array.CreateInstance(Type.GetType("System.Double"),3)
for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nN3 чекитти танда: ")):
	pnt3.SetValue(coord,i)

kancha2=doc.Utility.GetInteger(Prompt = "\nN2 менем N3 арасын канчага майдалайлы?: ")


pnt4 = Array.CreateInstance(Type.GetType("System.Double"),3)
for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nN4 чекитти танда: ")):
	pnt4.SetValue(coord,i)


def point_matrix(size1,size2):
	size3=3

	a=[]

	for i1 in range(0,size1):
		a.append([])
		for i2 in range (0,size2):
			a[i1].append([0,0,0])
	return a



P=point_matrix(kancha2+1,kancha+1)

x1=pnt1[0]
y1=pnt1[1]
z=pnt1[2]

x2=pnt2[0]
y2=pnt2[1]

x3=pntA[0]
y3=pntA[1]

doc.Utility.Prompt("\nx1="+str(x1))	
doc.Utility.Prompt("\ny1="+str(y1))	


doc.Utility.Prompt("\nx2="+str(x2))	
doc.Utility.Prompt("\ny2="+str(y2))	

x0 = 0.5*(-y1*x2**2+y3*x2**2-y2**2*y1+y2*y1**2+y1*x3**2+y3**2*y1-x1**2*y3+x1**2*y2-y3*y1**2+y2**2*y3-x3**2*y2-y2*y3**2)/(x1*y2-x1*y3-y1*x2+y1*x3-x3*y2+y3*x2)
y0 = -0.5*(x1**2*x2-x1**2*x3-x1*x2**2-x1*y2**2+x1*x3**2+x1*y3**2+y1**2*x2-y1**2*x3-x3**2*x2+x3*x2**2+x3*y2**2-y3**2*x2)/(x1*y2-x1*y3-y1*x2+y1*x3-x3*y2+y3*x2)


doc.Utility.Prompt("\nx0="+str(x0))	
doc.Utility.Prompt("\ny0="+str(y0))	
doc.Utility.Prompt("\nz="+str(z))	


#x0 = (1/2)*(-y1*x2^2+y3*x2^2-y2^2*y1+y2*y1^2+y1*x3^2+y3^2*y1-x1^2*y3+x1^2*y2-y3*y1^2+y2^2*y3-x3^2*y2-y2*y3^2)/(x1*y2-x1*y3-y1*x2+y1*x3-x3*y2+y3*x2)
#y0 = -(1/2)*(x1^2*x2-x1^2*x3-x1*x2^2-x1*y2^2+x1*x3^2+x1*y3^2+y1^2*x2-y1^2*x3-x3^2*x2+x3*x2^2+x3*y2^2-y3^2*x2)/(x1*y2-x1*y3-y1*x2+y1*x3-x3*y2+y3*x2)


pnt0=make_point(x0,y0,z)

R=dist(pnt1,pnt0)
doc.Utility.Prompt("\nR="+str(R))	


c=doc.ModelSpace.AddCircle(pnt0,R)



alpha=math.atan((y1-y0)/(x1-x0))
betta=math.atan((y2-y0)/(x2-x0))

if (x1-x0)<0:
	alpha+=math.pi

if (x2-x0)<0:
	betta+=math.pi

doc.Utility.Prompt("\nalpha="+str(alpha))	
doc.Utility.Prompt("\nbetta="+str(betta))	


kadam=(betta-alpha)/kancha

for i in range(0,kancha+1):
	burch=kadam*i+alpha
	x=x0+R*math.cos(burch)
	y=y0+R*math.sin(burch)
	#doc.Utility.Prompt("\ni="+str(i)+" x="+str(x)+" y="+str(y)+" burch="+str(burch/3.141592*180.0))	
	#pnt=make_point(x,y,z)
	#chekit=doc.ModelSpace.AddPoint(pnt)
	#txt=doc.ModelSpace.AddText(str(i),pnt,10)

	P[0][i][0]=x
	P[0][i][1]=y
	P[0][i][2]=z


P[kancha2][kancha][0]=pnt3[0]
P[kancha2][kancha][1]=pnt3[1]
P[kancha2][kancha][2]=pnt3[2]

#p1=make_point(P[kancha2][kancha][0],P[kancha2][kancha][1],P[kancha2][kancha][2])
#point=doc.ModelSpace.AddPoint(p1)

#в автокаде точки нумеруются задаются по кругу, а в массиве зигзагом

P[kancha2][0][0]=pnt4[0]
P[kancha2][0][1]=pnt4[1]
P[kancha2][0][2]=pnt4[2]

#p1=make_point(P[kancha2][0][0],P[kancha2][0][1],P[kancha2][0][2])
#point=doc.ModelSpace.AddPoint(p1)


for i in range(1,kancha):

	P[kancha2][i][0]=interpol(P[kancha2][0][0],P[kancha2][kancha][0],i,kancha)
	P[kancha2][i][1]=interpol(P[kancha2][0][1],P[kancha2][kancha][1],i,kancha)
	P[kancha2][i][2]=interpol(P[kancha2][0][2],P[kancha2][kancha][2],i,kancha)
	#p1=make_point(P[kancha2][i][0],P[kancha2][i][1],P[kancha2][i][2])
	#point=doc.ModelSpace.AddPoint(p1)

for i in range(0,kancha+1):
	for j in range(1,kancha2):
		P[j][i][0]=interpol(P[0][i][0],P[kancha2][i][0],j,kancha2)
		P[j][i][1]=interpol(P[0][i][1],P[kancha2][i][1],j,kancha2)
		P[j][i][2]=interpol(P[0][i][2],P[kancha2][i][2],j,kancha2)

		#p1=make_point(P[j][i][0],P[j][i][1],P[j][i][2])
		#point=doc.ModelSpace.AddPoint(p1)

for i in range(0,kancha):
	for j in range(0,kancha2):
		p1=make_point(P[j][i][0],P[j][i][1],P[j][i][2])
		p2=make_point(P[j][i+1][0],P[j][i+1][1],P[j][i+1][2])
		p3=make_point(P[j+1][i+1][0],P[j+1][i+1][1],P[j+1][i+1][2])
		p4=make_point(P[j+1][i][0],P[j+1][i][1],P[j+1][i][2])
		face=doc.ModelSpace.Add3DFace(p1,p2,p3,p4)


#углы только до 180 градусов


#когда оба больше 180 градусов, рисует наоборот