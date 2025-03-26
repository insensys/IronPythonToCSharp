import math



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


while (1==1):

	A = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nБиринчи чекитти танда: ")):
		A.SetValue(coord,i)

	B = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nЭкинчи чекитти танда: ")):
		B.SetValue(coord,i)

	kancha=doc.Utility.GetInteger(Prompt = "\nарасын канчага майдалайлы?: ")



	old=A
	#point=doc.ModelSpace.AddPoint(old)

	points=[A]

	for j in range(1,kancha+1):
		x=interpol(A[0],B[0],j,kancha)
		y=interpol(A[1],B[1],j,kancha)
		z=interpol(A[2],B[2],j,kancha)

		p=make_point(x,y,z)
		points.append(p)
		#point=doc.ModelSpace.AddPoint(p)
		#line=doc.ModelSpace.AddLine(old,p)
		#old=p

	for p in points:
		point=doc.ModelSpace.AddPoint(p)

