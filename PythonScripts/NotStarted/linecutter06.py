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

def interpol(n1,n2,kaisy,kancha):
	return  (n2-n1)*kaisy/kancha+n1


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

        layers=[]
	lt=tr.GetObject(db.LayerTableId, OpenMode.ForRead)
	for ltr in lt:
		lr=tr.GetObject(ltr, OpenMode.ForRead)
		layers.append( lr.Name)

	starts=[]
	ends=[]

        for layer in layers:


                for sel1 in per.Value:
                        ent = tr.GetObject(sel1.ObjectId, OpenMode.ForRead)
                        if type(ent) is Line and ent.Layer==layer:
				starts.append([ent.StartPoint[0],ent.StartPoint[1],ent.StartPoint[2]])
				ends.append([ent.EndPoint[0],ent.EndPoint[1],ent.EndPoint[2]])

                        if type(ent) is Face and ent.Layer==layer:
                                A=ent.GetVertexAt(0)
                                B=ent.GetVertexAt(1)
                                C=ent.GetVertexAt(2)
                                D=ent.GetVertexAt(3)


				starts.append([A[0],A[1],A[2]])
				ends.append([B[0],B[1],B[2]])

				starts.append([B[0],B[1],B[2]])
				ends.append([C[0],C[1],C[2]])

				starts.append([C[0],C[1],C[2]])
				ends.append([D[0],D[1],D[2]])

				starts.append([D[0],D[1],D[2]])
				ends.append([A[0],A[1],A[2]])

        
	tr.Commit() 


def lines_intersect(A,B,C,D):
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


	denom = (-yB*xD+yB*xC+yA*xD-yA*xC-yD*xA+yD*xB+yC*xA-yC*xB)
	
	return  denom

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


def select_points_between_2_points(pnt1,pnt2,points):
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


for i in range(0,len(starts)):
	A=make_point(starts[i][0],starts[i][1],starts[i][2])
	B=make_point(ends[i][0],ends[i][1],ends[i][2])

	points=[]
	AB=dist(A,B)

	for j in range(i+1,len(starts)):

		if i!=j:
			C=make_point(starts[j][0],starts[j][1],starts[j][2])
			D=make_point(ends[j][0],ends[j][1],ends[j][2])
			CD=dist(C,D)
                        denom=lines_intersect(A,B,C,D)
			if abs(denom)>pushinka:
				a=1
				F=point_between_4_points(A,B,C,D)
				AF=dist(A,F)
				FB=dist(F,B)

				CF=dist(C,F)
				FD=dist(F,D)

				
				if abs(AF)>pushinka and abs(FB)>pushinka and  abs(CF)>pushinka and abs(FD)>pushinka:
                                   if abs(AF+FB-AB)<pushinka and abs(CF+FD-CD)<pushinka:
					points.append(F)


	checked_points=select_points_between_2_points(A,B,points)

	for j in range(0,len(checked_points)):
		pnt=make_point(checked_points[j][0],checked_points[j][1],checked_points[j][2])
		point=doc.ModelSpace.AddPoint(pnt)
	

	if len(checked_points)>0:
		pnt=make_point(checked_points[0][0],checked_points[0][1],checked_points[0][2])
		point=doc.ModelSpace.AddPoint(pnt)

		#line=doc.ModelSpace.AddLine(A,pnt)


 		old=pnt
		if len(checked_points)>1:
			for j in range(1,len(checked_points)):
				pnt=make_point(checked_points[j][0],checked_points[j][1],checked_points[j][2])
			        #line=doc.ModelSpace.AddLine(old,pnt)
				mid=make_point((old[0]+pnt[0])/2,(old[1]+pnt[1])/2,(old[2]+pnt[2])/2)
				#text=doc.ModelSpace.AddText(str(i)+" "+str(j),mid,0.2)
				old=pnt
				
				
		if len(checked_points)>1:
			pnt=make_point(checked_points[-1][0],checked_points[-1][1],checked_points[-1][2])
		#line=doc.ModelSpace.AddLine(pnt,B)

				
	#else:
		#line=doc.ModelSpace.AddLine(A,B)

