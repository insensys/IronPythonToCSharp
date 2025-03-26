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
	doc.Utility.Prompt("\ni="+str(i)+" x="+str(x)+" y="+str(y)+" burch="+str(burch/3.141592*180.0))	
	pnt=make_point(x,y,z)
	chekit=doc.ModelSpace.AddPoint(pnt)
	txt=doc.ModelSpace.AddText(str(i),pnt,10)



#углы только до 180 градусов