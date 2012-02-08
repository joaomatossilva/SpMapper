 __                                          
/ _\_ __   /\/\   __ _ _ __  _ __   ___ _ __ 
\ \| '_ \ /    \ / _` | '_ \| '_ \ / _ \ '__|
_\ \ |_) / /\/\ \ (_| | |_) | |_) |  __/ |   
\__/ .__/\/    \/\__,_| .__/| .__/ \___|_|   
   |_|                |_|   |_|              

   
TOC
1. Introduction
2. Quick Usage
3. RoadMap
4. Version History

1. Introduction
-------------------------

SpMapper is a simple object mapper for Sharepoint lists.
Why should you use this instead Linq for Sharepoint or any other full ORM with Sharepoint support?
Simple... You shouldn't. 

But if you want a simple access to data on a list, try it out. It's simple and (not so...) fast.


2. Quick Usage
-------------------------

	using(var site = new SPSite("http://localhost:8090"))
	using (var web = site.OpenWeb()) {
		var list = web.Lists["Mapper"];
		var spQuery = new SPQuery();
		var items = list.Query<TestClass>(spQuery).ToList();

		/* use your items */
	}

	
3. RoadMap
-------------------------

This is what I imagine for Version 1.0:
- Query over a list
- Insert an item / items on a list
- Delete item / items on a list probably based on a query or item id?
- Update item based on item id?
- Suport for also Sahrepoint client object model
- Support for Sharepoint 2007

4. Version History
-------------------------

v0.1
2012-02-08
First public apha version.
Only supports query method and sharepoint 2010 object model