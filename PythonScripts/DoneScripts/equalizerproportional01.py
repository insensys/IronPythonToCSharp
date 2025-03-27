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


def select_points_between_2_points(pnt1,pnt2):
	L=dist(pnt1,pnt2)

	selected_points=[]
	for p in points:
		A=dist(pnt1,p)
		B=dist(p,pnt2)
		C=L-A-B
		if abs(C)<pushinka:
			selected_points.append([p,A,B,C])



	distances=[]
	for p in selected_points:
		if abs(p[1])>pushinka and abs(p[2])>pushinka:
			distances.append(p[1])	

	a=set(distances)
	sorted_distances=list(a)
	sorted_distances.sort()


	midpoints=[]
	for d in sorted_distances:
		j=0
		for p in selected_points:
			if j==0 and abs(d-p[1])<pushinka:
				j+=1
				midpoints.append(p[0])


	checkedpoints=[]
	i=0
	for m in midpoints:
		if i==0:
			checkedpoints.append(m)
		else:
			if dist(m,checkedpoints[-1])>pushinka:
				checkedpoints.append(m)
				

		i+=1

	return checkedpoints


def point_between_4_points(A,B,C,D):
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


	x = (xB*yA*xD-xB*yA*xC-xA*xC*yD-xB*yC*xD+xB*xC*yD+xA*yB*xC+xA*yC*xD-xA*yB*xD)/(-yB*xD+yB*xC+yA*xD-yA*xC-yD*xA+yD*xB+yC*xA-yC*xB)

	y = (yB*xC*yD+yC*xA*yB-yD*xA*yB-yB*yC*xD-yA*xC*yD+yD*yA*xB+yA*yC*xD-yC*yA*xB)/(-yB*xD+yB*xC+yA*xD-yA*xC-yD*xA+yD*xB+yC*xA-yC*xB)

	z = (-yD*xA*zB+yA*xD*zB+yC*xA*zB-yA*xC*zB-xD*zA*yB-zA*yC*xB-zA*xC*yD+zA*yC*xD-zB*yC*xD+zB*xC*yD+xC*zA*yB+zA*yD*xB)/(-yB*xD+yB*xC+yA*xD-yA*xC-yD*xA+yD*xB+yC*xA-yC*xB)

	return [x,y,z]



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

                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is DBPoint and ent.Layer==layer:
				points.append([ent.Position[0],ent.Position[1],ent.Position[2]])

                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Line and ent.Layer==layer:
				points.append([ent.StartPoint[0],ent.StartPoint[1],ent.StartPoint[2]])
				points.append([ent.EndPoint[0],ent.EndPoint[1],ent.EndPoint[2]])

                i=0
                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Face and ent.Layer==layer:
                                p=ent.GetVertexAt(0)

				points.append([p[0],p[1],p[2]])

                                p=ent.GetVertexAt(1)

				points.append([p[0],p[1],p[2]])

                                p=ent.GetVertexAt(2)

				points.append([p[0],p[1],p[2]])

                                p=ent.GetVertexAt(3)

				points.append([p[0],p[1],p[2]])
        
	tr.Commit() 



	pnt1 = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nБиринчи чекитти танда: ")):
		pnt1.SetValue(coord,i)

	skolko=0

	pnt2 = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nЭкинчи чекитти танда: ")):
		pnt2.SetValue(coord,i)


	midpoints=select_points_between_2_points(pnt1,pnt2)

	if len(midpoints)>0:
		p11=make_point(midpoints[0][0],midpoints[0][1],midpoints[0][2])
		p22=make_point(midpoints[-1][0],midpoints[-1][1],midpoints[-1][2])

		#line=doc.ModelSpace.AddLine(pnt1,p11)
		#line=doc.ModelSpace.AddLine(p22,pnt2)

		
		if len(midpoints)>1:
			for i in range(1,len(midpoints)):

				p01=make_point(midpoints[i-1][0],midpoints[i-1][1],midpoints[i-1][2])
				p02=make_point(midpoints[i][0],midpoints[i][1],midpoints[i][2])
				#line=doc.ModelSpace.AddLine(p01,p02)
			skolko=len(midpoints)+1
		else:
			doc.Utility.Prompt("\nОртосунда бир эле чекит бар")	
			skolko=2
	else:
		doc.Utility.Prompt("\nОртосунда чекиттер жок")	
		skolko=1

	doc.Utility.Prompt("\nskolko="+str(skolko))	
	
	
	#print dir(doc)


	#выделить точки, линии, фейсы

	#занести координаты точек в список


	#выделить две точки

	#проверять на чужие, ести такие тчки попадаются, заносить их в другой список 
	#(добавить растояние до первой точки)

	#проверять дубликаты

	#сортировать расстояния до первой точки
	#перестроить список по этому принципу

	#нарисовать точки по порядку



	pnt3 = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nN3 чекитти танда: ")):
		pnt3.SetValue(coord,i)


	pnt4 = Array.CreateInstance(Type.GetType("System.Double"),3)
	for i,coord in enumerate(doc.Utility.GetPoint(Prompt = "\nN4 чекитти танда: ")):
		pnt4.SetValue(coord,i)

	midpoints2=select_points_between_2_points(pnt2,pnt3)
	skolko2=len(midpoints2)+1



	def point_matrix(size1,size2):
		size3=3

		a=[]

		for i1 in range(0,size1):
			a.append([])
			for i2 in range (0,size2):
				a[i1].append([0,0,0])
		return a


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

	L=dist(P[0][0],P[0][skolko])
	#print("L="+str(L))
	#print("skolko="+str(skolko))

	for i in range(1,skolko):
		#P[0][i][0]=interpol(P[0][0][0],P[0][skolko][0],i,skolko)
		#P[0][i][1]=interpol(P[0][0][1],P[0][skolko][1],i,skolko)
		#P[0][i][2]=interpol(P[0][0][2],P[0][skolko][2],i,skolko)
                #print("len(P[0])="+str(len(P[0])))
                #print("len(midpoints)="+str(len(midpoints)))

		P[0][i][0]=midpoints[i-1][0]
		P[0][i][1]=midpoints[i-1][1]
		P[0][i][2]=midpoints[i-1][2]
                
                #print("i="+str(i))

		Li=dist(P[0][0],P[0][i])
                #print("Li="+str(Li))

		#p1=make_point(P[0][i][0],P[0][i][1],P[0][i][2])
		#point=doc.ModelSpace.AddPoint(p1)

		P[skolko2][i][0]=interpol(P[skolko2][0][0],P[skolko2][skolko][0],Li,L)
		P[skolko2][i][1]=interpol(P[skolko2][0][1],P[skolko2][skolko][1],Li,L)
		P[skolko2][i][2]=interpol(P[skolko2][0][2],P[skolko2][skolko][2],Li,L)

		#p1=make_point(P[skolko2][i][0],P[skolko2][i][1],P[skolko2][i][2])
		#point=doc.ModelSpace.AddPoint(p1)


	L2=dist(P[0][skolko],P[skolko2][skolko])
	#print("L2="+str(L2))
	for j in range(1,skolko2):

		P[j][skolko][0]=midpoints2[j-1][0]
		P[j][skolko][1]=midpoints2[j-1][1]
		P[j][skolko][2]=midpoints2[j-1][2]

		#p1=make_point(P[j][skolko][0],P[j][skolko][1],P[j][skolko][2])
		#point=doc.ModelSpace.AddPoint(p1)
                Lj=dist(P[0][skolko],P[j][skolko])

		P[j][0][0]=interpol(P[0][0][0],P[skolko2][0][0],Lj,L2)
		P[j][0][1]=interpol(P[0][0][1],P[skolko2][0][1],Lj,L2)
		P[j][0][2]=interpol(P[0][0][2],P[skolko2][0][2],Lj,L2)

		#p1=make_point(P[j][0][0],P[j][0][1],P[j][0][2])
		#point=doc.ModelSpace.AddPoint(p1)


	for i in range(1,skolko):
		for j in range(1,skolko2):
			#P[j][i]=point_between_4_points(P[0][i],P[skolko2][i],P[j][0],P[j][skolko])
			np=point_between_4_points(P[0][i],P[skolko2][i],P[j][0],P[j][skolko])
                        P[j][i]=np
			
			#p1=make_point(P[j][i][0],P[j][i][1],P[j][i][2])
			#p1=make_point(np[0],np[1],np[2])
			#point=doc.ModelSpace.AddPoint(p1)


	for i in range(0,skolko):
		for j in range(0,skolko2):
			p1=make_point(P[j][i][0],P[j][i][1],P[j][i][2])
			p2=make_point(P[j][i+1][0],P[j][i+1][1],P[j][i+1][2])
			p3=make_point(P[j+1][i+1][0],P[j+1][i+1][1],P[j+1][i+1][2])
			p4=make_point(P[j+1][i][0],P[j+1][i][1],P[j+1][i][2])
			face=doc.ModelSpace.Add3DFace(p1,p2,p3,p4)


#найти точки, лежашие между второй и третьей точками
#задать точки, лежашие между первой и четвертой точками
#остальные вычислить через новую функцию