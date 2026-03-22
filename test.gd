extends SceneTree
func _init():
    print("HAS_REQUEST_DIR: ", OS.has_method("request_dir_access"))
    print("HAS_REQUEST_PERM: ", OS.has_method("request_permissions"))
    quit()
