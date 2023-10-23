variable "tenancy_ocid" {
  type = string
}

variable "user_ocid" {
  type = string
}

variable "private_key_path" {
  type = string
}

variable "fingerprint" {
  type = string
}

variable "region" {
  type    = string
  default = "eu-frankfurt-1"
}

variable "ssh_pub_key_path" {
  type = string
}

variable "image_source" {
  type    = string
  default = "ocid1.image.oc1.eu-frankfurt-1.aaaaaaaap362udryq4igudmoxz2b5hpkzlhrelii7beusqhps33mxfwn2izq"
}
