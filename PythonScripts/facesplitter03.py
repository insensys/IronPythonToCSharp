#import clr
#import System
#import System.Reflection.Assembly as Assembly


#from Autodesk.AutoCAD.ApplicationServices import *
#from  Autodesk.AutoCAD.DatabaseServices import *
#from Autodesk.AutoCAD.EditorInput import *
#from  Autodesk.AutoCAD.Runtime import * 

#doc = Application.DocumentManager.MdiActiveDocument
#ed = doc.Editor
#db = doc.Database

import math

pushinka=0.001

def dist(a,b):
	c = math.sqrt((b[0] - a[0])*(b[0] - a[0])+(b[1] - a[1])*(b[1] - a[1])+(b[2] - a[2])*(b[2] - a[2]))
	return c


import clr
import System
import System.Reflection.Assembly as Assembly


from Autodesk.AutoCAD.ApplicationServices import *
from  Autodesk.AutoCAD.DatabaseServices import *
from Autodesk.AutoCAD.EditorInput import *
from  Autodesk.AutoCAD.Runtime import * 


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

standalone = False
# standalone = True


def interpol(n1,n2,kaisy,kancha):
	return  (n2-n1)*kaisy/kancha+n1


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


docNet = Application.DocumentManager.MdiActiveDocument
ed = docNet.Editor
db = docNet.Database
	  
kancha=doc.Utility.GetInteger(Prompt = "\nканчага майдалайлы?: ")

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

vert0=[]
vert1=[]
vert2=[]
vert3=[]

tri0=[]
tri1=[]
tri2=[]

opts = PromptSelectionOptions()
opts.MessageForAdding = "Объекттерди танда"
per = ed.GetSelection(opts)
if per.Status == PromptStatus.OK:

		
	print "okey, selected"
	print per.Value.Count
	tr = db.TransactionManager.StartTransaction()

	points=[]
        layers=[]
	lt=tr.GetObject(db.LayerTableId, OpenMode.ForRead)
	for ltr in lt:
		lr=tr.GetObject(ltr, OpenMode.ForRead)
		layers.append( lr.Name)

        for layer in layers:


                i=0
                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Face and ent.Layer==layer:
                                p0=ent.GetVertexAt(0)
                                p1=ent.GetVertexAt(1)
                                p2=ent.GetVertexAt(2)
                                p3=ent.GetVertexAt(3)

				d01=dist(p0,p1)
				d12=dist(p1,p2)
				d23=dist(p2,p3)
				d30=dist(p3,p0)

				if d01>pushinka and d12>pushinka and d23>pushinka and  d30>pushinka:

					vert0.append([p0[0],p0[1],p0[2]])
					vert1.append([p1[0],p1[1],p1[2]])
					vert2.append([p2[0],p2[1],p2[2]])
					vert3.append([p3[0],p3[1],p3[2]])

				if d01<pushinka and d12>pushinka and d23>pushinka and  d30>pushinka:
					tri0.append([p0[0],p0[1],p0[2]])
					tri1.append([p2[0],p2[1],p2[2]])
					tri2.append([p3[0],p3[1],p3[2]])

				if d01>pushinka and d12<pushinka and d23>pushinka and  d30>pushinka:
					tri0.append([p0[0],p0[1],p0[2]])
					tri1.append([p1[0],p1[1],p1[2]])
					tri2.append([p3[0],p3[1],p3[2]])

				if d01>pushinka and d12>pushinka and d23<pushinka and  d30>pushinka:
					tri0.append([p0[0],p0[1],p0[2]])
					tri1.append([p1[0],p1[1],p1[2]])
					tri2.append([p2[0],p2[1],p2[2]])
					
				if d01>pushinka and d12>pushinka and d23>pushinka and  d30<pushinka:
					tri0.append([p1[0],p1[1],p1[2]])
					tri1.append([p2[0],p2[1],p2[2]])
					tri2.append([p3[0],p3[1],p3[2]])
        
	tr.Commit() 


kancha2=kancha

for v in range(0,len(vert0)):


	P=point_matrix(kancha2+1,kancha+1)

	P[0][0][0]=vert0[v][0]
	P[0][0][1]=vert0[v][1]
	P[0][0][2]=vert0[v][2]

	#p1=make_point(P[0][0][0],P[0][0][1],P[0][0][2])
	#point=doc.ModelSpace.AddPoint(p1)

	P[0][kancha][0]=vert1[v][0]
	P[0][kancha][1]=vert1[v][1]
	P[0][kancha][2]=vert1[v][2]

	#p1=make_point(P[0][kancha][0],P[0][kancha][1],P[0][kancha][2])
	#point=doc.ModelSpace.AddPoint(p1)

	P[kancha2][kancha][0]=vert2[v][0]
	P[kancha2][kancha][1]=vert2[v][1]
	P[kancha2][kancha][2]=vert2[v][2]

	#p1=make_point(P[kancha2][kancha][0],P[kancha2][kancha][1],P[kancha2][kancha][2])
	#point=doc.ModelSpace.AddPoint(p1)

	#в автокаде точки нумеруются задаются по кругу, а в массиве зигзагом

	P[kancha2][0][0]=vert3[v][0]
	P[kancha2][0][1]=vert3[v][1]
	P[kancha2][0][2]=vert3[v][2]

	#p1=make_point(P[kancha2][0][0],P[kancha2][0][1],P[kancha2][0][2])
	#point=doc.ModelSpace.AddPoint(p1)


	i=1
	while i<kancha:
		P[0][i][0]=interpol(P[0][0][0],P[0][kancha][0],i,kancha)
		P[0][i][1]=interpol(P[0][0][1],P[0][kancha][1],i,kancha)
		P[0][i][2]=interpol(P[0][0][2],P[0][kancha][2],i,kancha)
	#	p1=make_point(P[0][i][0],P[0][i][1],P[0][i][2])
	#	point=doc.ModelSpace.AddPoint(p1)

		P[kancha2][i][0]=interpol(P[kancha2][0][0],P[kancha2][kancha][0],i,kancha)
		P[kancha2][i][1]=interpol(P[kancha2][0][1],P[kancha2][kancha][1],i,kancha)
		P[kancha2][i][2]=interpol(P[kancha2][0][2],P[kancha2][kancha][2],i,kancha)
	#	p1=make_point(P[kancha2][i][0],P[kancha2][i][1],P[kancha2][i][2])
	#	point=doc.ModelSpace.AddPoint(p1)

		i+=1

	i=0
	while i<kancha+1:
		j=1
		while j<kancha2:
			P[j][i][0]=interpol(P[0][i][0],P[kancha2][i][0],j,kancha2)
			P[j][i][1]=interpol(P[0][i][1],P[kancha2][i][1],j,kancha2)
			P[j][i][2]=interpol(P[0][i][2],P[kancha2][i][2],j,kancha2)

	#		p1=make_point(P[j][i][0],P[j][i][1],P[j][i][2])
	#		point=doc.ModelSpace.AddPoint(p1)

			j+=1
		i+=1

	i=0
	while i<kancha:
		j=0
		while j<kancha2:
			p1=make_point(P[j][i][0],P[j][i][1],P[j][i][2])
			p2=make_point(P[j][i+1][0],P[j][i+1][1],P[j][i+1][2])
			p3=make_point(P[j+1][i+1][0],P[j+1][i+1][1],P[j+1][i+1][2])
			#face=doc.ModelSpace.Add3DFace(p1,p2,p3,p1)

			p4=make_point(P[j+1][i][0],P[j+1][i][1],P[j+1][i][2])
			face=doc.ModelSpace.Add3DFace(p1,p2,p3,p4)

			j+=1
		i+=1

	#face=doc.ModelSpace.Add3DFace(pnt1,pnt2,pnt3,pnt1)


print(len(tri0))

for v in range(0,len(tri0)):


	P=point_matrix(kancha2+1,kancha+1)

	P[0][0][0]=tri0[v][0]
	P[0][0][1]=tri0[v][1]
	P[0][0][2]=tri0[v][2]

	#p1=make_point(P[0][0][0],P[0][0][1],P[0][0][2])
	#point=doc.ModelSpace.AddPoint(p1)

	P[0][kancha][0]=tri1[v][0]
	P[0][kancha][1]=tri1[v][1]
	P[0][kancha][2]=tri1[v][2]

	#p1=make_point(P[0][kancha][0],P[0][kancha][1],P[0][kancha][2])
	#point=doc.ModelSpace.AddPoint(p1)

	P[kancha2][kancha][0]=tri2[v][0]+tri1[v][0]-tri0[v][0]
	P[kancha2][kancha][1]=tri2[v][1]+tri1[v][1]-tri0[v][1]
	P[kancha2][kancha][2]=tri2[v][2]+tri1[v][2]-tri0[v][2]

	#p1=make_point(P[kancha2][kancha][0],P[kancha2][kancha][1],P[kancha2][kancha][2])
	#point=doc.ModelSpace.AddPoint(p1)

	#в автокаде точки нумеруются задаются по кругу, а в массиве зигзагом

	P[kancha2][0][0]=tri2[v][0]
	P[kancha2][0][1]=tri2[v][1]
	P[kancha2][0][2]=tri2[v][2]

	#p1=make_point(P[kancha2][0][0],P[kancha2][0][1],P[kancha2][0][2])
	#point=doc.ModelSpace.AddPoint(p1)


	i=1
	while i<kancha:
		P[0][i][0]=interpol(P[0][0][0],P[0][kancha][0],i,kancha)
		P[0][i][1]=interpol(P[0][0][1],P[0][kancha][1],i,kancha)
		P[0][i][2]=interpol(P[0][0][2],P[0][kancha][2],i,kancha)
	#	p1=make_point(P[0][i][0],P[0][i][1],P[0][i][2])
	#	point=doc.ModelSpace.AddPoint(p1)

		P[kancha2][i][0]=interpol(P[kancha2][0][0],P[kancha2][kancha][0],i,kancha)
		P[kancha2][i][1]=interpol(P[kancha2][0][1],P[kancha2][kancha][1],i,kancha)
		P[kancha2][i][2]=interpol(P[kancha2][0][2],P[kancha2][kancha][2],i,kancha)
	#	p1=make_point(P[kancha2][i][0],P[kancha2][i][1],P[kancha2][i][2])
	#	point=doc.ModelSpace.AddPoint(p1)

		i+=1

	i=0
	while i<kancha+1:
		j=1
		while j<kancha2:
			P[j][i][0]=interpol(P[0][i][0],P[kancha2][i][0],j,kancha2)
			P[j][i][1]=interpol(P[0][i][1],P[kancha2][i][1],j,kancha2)
			P[j][i][2]=interpol(P[0][i][2],P[kancha2][i][2],j,kancha2)

	#		p1=make_point(P[j][i][0],P[j][i][1],P[j][i][2])
	#		point=doc.ModelSpace.AddPoint(p1)

			j+=1
		i+=1

	for i in range(0,kancha):
		for j in range(0,kancha2):
			p1=make_point(P[j][i][0],P[j][i][1],P[j][i][2])
			p2=make_point(P[j][i+1][0],P[j][i+1][1],P[j][i+1][2])
			p3=make_point(P[j+1][i+1][0],P[j+1][i+1][1],P[j+1][i+1][2])

			p4=make_point(P[j+1][i][0],P[j+1][i][1],P[j+1][i][2])


			#p124=make_point((p1[0]+p2[0]+p4[0])/3,(p1[1]+p2[1]+p4[1])/3,(p1[2]+p2[2]+p4[2])/3)
			#text=doc.ModelSpace.AddText("a "+str(i)+" "+str(j),p124,0.2)

			#p243=make_point((p2[0]+p4[0]+p3[0])/3,(p2[1]+p4[1]+p3[1])/3,(p2[2]+p4[2]+p3[2])/3)
			#text=doc.ModelSpace.AddText("b "+str(i)+" "+str(j),p243,0.2)


			if i+j<kancha:
				face=doc.ModelSpace.Add3DFace(p1,p2,p4,p1)
			if i+j<kancha-1:
				face=doc.ModelSpace.Add3DFace(p2,p4,p3,p2)


	#face=doc.ModelSpace.Add3DFace(pnt1,pnt2,pnt3,pnt1)


