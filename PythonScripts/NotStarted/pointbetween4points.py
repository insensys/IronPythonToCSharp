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
for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nВыберите первую точку: ")):
	pnt1.SetValue(coord,i)

pnt2 = Array.CreateInstance(Type.GetType("System.Double"),3)
for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nВыберите вторую точку: ")):
	pnt2.SetValue(coord,i)


pnt3 = Array.CreateInstance(Type.GetType("System.Double"),3)
for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nВыберите третью точку: ")):
	pnt3.SetValue(coord,i)

pnt4 = Array.CreateInstance(Type.GetType("System.Double"),3)
for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nВыберите четвертую точку: ")):
	pnt4.SetValue(coord,i)


def point_berween_4_points(A,B,C,D):
	xA=A[0]
	yA=A[1]
	zA=A[2]
	
	xB=B[0]
	yB=B[1]
	zB=B[2]

	xC=C[0]
	yC=C[1]
	zC=C[2]

	xD=D[0]
	yD=D[1]
	zD=D[2]

	demodX=(-yB*xD+yB*xC+yA*xD-yA*xC-yD*xA+yD*xB+yC*xA-yC*xB)
	demodY=(-yB*xD+yB*xC+yA*xD-yA*xC-yD*xA+yD*xB+yC*xA-yC*xB)
	demodZ=(-yB*xD+yB*xC+yA*xD-yA*xC-yD*xA+yD*xB+yC*xA-yC*xB)

	print(A,B,C,D)
	print(demodX,demodY,demodZ)

	x = (xB*yA*xD-xB*yA*xC-xA*xC*yD-xB*yC*xD+xB*xC*yD+xA*yB*xC+xA*yC*xD-xA*yB*xD)/(-yB*xD+yB*xC+yA*xD-yA*xC-yD*xA+yD*xB+yC*xA-yC*xB)

	y = (yB*xC*yD+yC*xA*yB-yD*xA*yB-yB*yC*xD-yA*xC*yD+yD*yA*xB+yA*yC*xD-yC*yA*xB)/(-yB*xD+yB*xC+yA*xD-yA*xC-yD*xA+yD*xB+yC*xA-yC*xB)

	z = (-yD*xA*zB+yA*xD*zB+yC*xA*zB-yA*xC*zB-xD*zA*yB-zA*yC*xB-zA*xC*yD+zA*yC*xD-zB*yC*xD+zB*xC*yD+xC*zA*yB+zA*yD*xB)/(-yB*xD+yB*xC+yA*xD-yA*xC-yD*xA+yD*xB+yC*xA-yC*xB)

	return [x,y,z]


p=make_point(point_berween_4_points(pnt1,pnt2,pnt3,pnt4))

point=doc.ModelSpace.AddPoint(p)

