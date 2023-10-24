

resource "oci_core_instance" "web01_instance" {
  # Required
  availability_domain = data.oci_identity_availability_domains.ads.availability_domains[0].name
  compartment_id      = oci_identity_compartment.tf-compartment.id
  shape               = "VM.Standard.A1.Flex"
  shape_config {
    memory_in_gbs = 6
    ocpus         = 1
  }
  source_details {
    source_id   = var.image_source
    source_type = "image"
  }

  # Optional
  display_name = "recipesapi-web01"
  create_vnic_details {
    assign_public_ip = true
    subnet_id        = oci_core_subnet.vcn-public-subnet.id
  }
  metadata = {
    ssh_authorized_keys = file(var.ssh_pub_key_path)
  }
  preserve_boot_volume = false
}
