
resource "oci_identity_compartment" "tf-compartment" {
  # Required
  compartment_id = var.tenancy_ocid
  description    = "Compartment for recipesapi resources."
  name           = "recipesapi"
}
