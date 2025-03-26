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

def point_matrix(size1,size2):
	size3=3

	a=[]

	i1=0
	while i1<size1:
		a.append([])
		i2=0
		while i2<size2:
			a[i1].append([0,0,0])
			i2+=1
		i1+=1
	return a


while (1==1):

	pnt1 = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nВыберите первую точку: ")):
		pnt1.SetValue(coord,i)

	pnt2 = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nВыберите вторую точку: ")):
		pnt2.SetValue(coord,i)

	skolko=doc.Utility.GetInteger(Prompt = "\nНа сколько делить между первой и второй точками: ")


	pnt3 = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nВыберите третью точку: ")):
		pnt3.SetValue(coord,i)

	skolko2=doc.Utility.GetInteger(Prompt = "\nНа сколько делить между второй и третьей точками: ")


	pnt4 = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nВыберите четвертую точку: ")):
		pnt4.SetValue(coord,i)




	P=point_matrix(skolko2+1,skolko+1)

	P[0][0][0]=pnt1[0]
	P[0][0][1]=pnt1[1]
	P[0][0][2]=pnt1[2]

	#p1=make_point(P[0][0][0],P[0][0][1],P[0][0][2])
	#point=doc.ModelSpace.AddPoint(p1)

	P[0][skolko][0]=pnt2[0]
	P[0][skolko][1]=pnt2[1]
	P[0][skolko][2]=pnt2[2]

	#p1=make_point(P[0][skolko][0],P[0][skolko][1],P[0][skolko][2])
	#point=doc.ModelSpace.AddPoint(p1)

	P[skolko2][skolko][0]=pnt3[0]
	P[skolko2][skolko][1]=pnt3[1]
	P[skolko2][skolko][2]=pnt3[2]

	#p1=make_point(P[skolko2][skolko][0],P[skolko2][skolko][1],P[skolko2][skolko][2])
	#point=doc.ModelSpace.AddPoint(p1)

	#в автокаде точки нумеруются задаются по кругу, а в массиве зигзагом

	P[skolko2][0][0]=pnt4[0]
	P[skolko2][0][1]=pnt4[1]
	P[skolko2][0][2]=pnt4[2]

	#p1=make_point(P[skolko2][0][0],P[skolko2][0][1],P[skolko2][0][2])
	#point=doc.ModelSpace.AddPoint(p1)


	i=1
	while i<skolko:
		P[0][i][0]=interpol(P[0][0][0],P[0][skolko][0],i,skolko)
		P[0][i][1]=interpol(P[0][0][1],P[0][skolko][1],i,skolko)
		P[0][i][2]=interpol(P[0][0][2],P[0][skolko][2],i,skolko)
	#	p1=make_point(P[0][i][0],P[0][i][1],P[0][i][2])
	#	point=doc.ModelSpace.AddPoint(p1)

		P[skolko2][i][0]=interpol(P[skolko2][0][0],P[skolko2][skolko][0],i,skolko)
		P[skolko2][i][1]=interpol(P[skolko2][0][1],P[skolko2][skolko][1],i,skolko)
		P[skolko2][i][2]=interpol(P[skolko2][0][2],P[skolko2][skolko][2],i,skolko)
	#	p1=make_point(P[skolko2][i][0],P[skolko2][i][1],P[skolko2][i][2])
	#	point=doc.ModelSpace.AddPoint(p1)

		i+=1

	i=0
	while i<skolko+1:
		j=1
		while j<skolko2:
			P[j][i][0]=interpol(P[0][i][0],P[skolko2][i][0],j,skolko2)
			P[j][i][1]=interpol(P[0][i][1],P[skolko2][i][1],j,skolko2)
			P[j][i][2]=interpol(P[0][i][2],P[skolko2][i][2],j,skolko2)

	#		p1=make_point(P[j][i][0],P[j][i][1],P[j][i][2])
	#		point=doc.ModelSpace.AddPoint(p1)

			j+=1
		i+=1

	i=0
	while i<skolko:
		j=0
		while j<skolko2:
			p1=make_point(P[j][i][0],P[j][i][1],P[j][i][2])
			p2=make_point(P[j][i+1][0],P[j][i+1][1],P[j][i+1][2])
			p3=make_point(P[j+1][i+1][0],P[j+1][i+1][1],P[j+1][i+1][2])
			#face=doc.ModelSpace.Add3DFace(p1,p2,p3,p1)

			p4=make_point(P[j+1][i][0],P[j+1][i][1],P[j+1][i][2])
			face=doc.ModelSpace.Add3DFace(p1,p2,p3,p4)

			j+=1
		i+=1

	#face=doc.ModelSpace.Add3DFace(pnt1,pnt2,pnt3,pnt1)

	doc.Utility.Prompt("\nДаяр болду")	
