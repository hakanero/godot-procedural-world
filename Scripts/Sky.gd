extends CanvasItem

export var cloud_speed : float 
onready var global_v=get_tree().get_root().get_node("World")

func _process(_delta):
	self.material.set("shader_param/iTime",global_v.iTime*cloud_speed)
	self.material.set("shader_param/iFrame",global_v.iFrame)
