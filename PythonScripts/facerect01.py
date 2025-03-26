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
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nA: ")):
		A.SetValue(coord,i)

	B = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nB: ")):
		B.SetValue(coord,i)


	C = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nC: ")):
		C.SetValue(coord,i)



	AB=[B[0]-A[0],B[1]-A[1],B[2]-A[2]]
	#BC=[C[0]-B[0],C[1]-B[1],C[2]-B[2]]


	D=make_point(C[0]-AB[0],C[1]-AB[1],C[2]-AB[2])


	face=doc.ModelSpace.Add3DFace(A,B,C,D)

