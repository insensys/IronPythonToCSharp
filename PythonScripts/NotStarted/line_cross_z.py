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

z=doc.Utility.GetReal(Prompt = "\nZ: ")



def point_between_2_points_at_z(A,B,z):
  ax=B[0]-A[0]
  ay=B[1]-A[1]
  az=B[2]-A[2]

  x1=A[0]
  y1=A[1]
  z1=A[2]

  x=ax/az*(z-z1)+x1
  y=ay/az*(z-z1)+y1

  return [x,y,z]


p=point_between_2_points_at_z(pnt1,pnt2,z)
point=doc.ModelSpace.AddPoint(make_point(p[0],p[1],p[2]))

