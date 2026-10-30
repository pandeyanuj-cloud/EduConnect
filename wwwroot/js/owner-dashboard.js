const ownerState = { classes: [], editingId: 0 };

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;" })[character]);
}

function ownerUser() {
  try { return JSON.parse(localStorage.getItem("eduConnectUser") || "null"); } catch { return null; }
}

function money(value) { return `₹${Number(value || 0).toLocaleString("en-IN")}`; }

function showOwnerName() {
  const user = ownerUser();
  const element = document.getElementById("ownerName");
  if (element && user?.fullName) element.textContent = user.fullName;
}

function logout() {
  localStorage.removeItem("eduConnectUser");
  window.location.href = "login.html";
}

function classImage(item) {
  return escapeHtml(item.imageUrl || "https://placehold.co/800x450/eaf2f6/17324d?text=EduConnect");
}

function renderOwnerClasses() {
  const container = document.getElementById("ownerClassCards");
  if (!container) return;
  document.getElementById("classCount").textContent = ownerState.classes.length;
  if (!ownerState.classes.length) {
    container.innerHTML = "<p class='empty-state'>No classes yet. Add your first class using the form.</p>";
    return;
  }
  container.innerHTML = ownerState.classes.map((item) => `<article class="class-card owner-class-card">
    <img class="class-image" src="${classImage(item)}" alt="${escapeHtml(item.subject || "Class")}" />
    <div class="card-body"><div class="card-topline"><div><h3>${escapeHtml(item.subject || "Class")}</h3><span>${escapeHtml(item.location || "Location not provided")}</span></div><span class="rating-pill">★ ${Number(item.rating || 0).toFixed(1)}</span></div>
    <div class="meta-tags"><span>${escapeHtml(item.standard || "All levels")}</span><span>${escapeHtml(item.mode || "Both")}</span></div><p class="class-description">${escapeHtml(item.description || "No description added yet.")}</p>${item.materialProvided ? `<p class="class-material"><strong>Material:</strong> ${escapeHtml(item.materialProvided)}</p>` : ""}
    ${item.contactNumber || item.classTimings ? `<p class="class-material">${item.contactNumber ? `<strong>Contact:</strong> ${escapeHtml(item.contactNumber)}` : ""}${item.contactNumber && item.classTimings ? " &bull; " : ""}${item.classTimings ? `<strong>Timings:</strong> ${escapeHtml(item.classTimings)}` : ""}</p>` : ""}
    <div class="card-footer"><div class="price-block"><strong>${money(item.fee)}</strong><span>/ Year</span></div><div class="action-stack"><button class="mini-btn ghost" type="button" data-edit-class="${item.classId}">Edit</button><button class="mini-btn delete-btn" type="button" data-delete-class="${item.classId}">Delete</button></div></div></div></article>`).join("");
}

async function loadOwnerClasses() {
  const response = await fetch("/api/owner/classes");
  if (response.status === 401) { logout(); return; }
  const data = await response.json();
  ownerState.classes = Array.isArray(data) ? data : [];
  renderOwnerClasses();
}

function resetClassForm() {
  ownerState.editingId = 0;
  document.getElementById("classForm")?.reset();
  document.getElementById("classId").value = "0";
  document.getElementById("rating").value = "0";
  document.getElementById("formTitle").textContent = "Add a class";
  document.getElementById("formEyebrow").textContent = "New listing";
  document.getElementById("saveClassBtn").textContent = "Save class";
  document.getElementById("cancelEdit").classList.add("hidden");
  document.getElementById("imagePreview").src = "";
  document.getElementById("imagePreview").classList.add("hidden");
  document.getElementById("imageFile").value = "";
  document.getElementById("imageFileName").textContent = "No image selected";
  document.getElementById("materialProvided").value = "";
  document.getElementById("imageUploadBox").classList.remove("hidden");
  document.getElementById("changeImageBtn").classList.add("hidden");
}

function editClass(classId) {
  const item = ownerState.classes.find((entry) => entry.classId === classId);
  if (!item) return;
  ownerState.editingId = classId;
  document.getElementById("classId").value = classId;
  document.getElementById("subject").value = item.subject || "";
  document.getElementById("fee").value = item.fee || 0;
  document.getElementById("location").value = item.location || "";
  document.getElementById("standard").value = item.standard || "";
  document.getElementById("rating").value = item.rating || 0;
  document.getElementById("mode").value = item.mode || "Both";
  document.getElementById("imageUrl").value = item.imageUrl || "";
  const preview = document.getElementById("imagePreview");
  preview.src = item.imageUrl || "";
  preview.classList.toggle("hidden", !item.imageUrl);
  document.getElementById("imageUploadBox").classList.toggle("hidden", Boolean(item.imageUrl));
  document.getElementById("changeImageBtn").classList.toggle("hidden", !item.imageUrl);
  document.getElementById("description").value = item.description || "";
  document.getElementById("materialProvided").value = item.materialProvided || "";
  document.getElementById("contactNumber").value = item.contactNumber || "";
  document.getElementById("classTimings").value = item.classTimings || "";
  document.getElementById("formTitle").textContent = "Edit class";
  document.getElementById("formEyebrow").textContent = "Update listing";
  document.getElementById("saveClassBtn").textContent = "Save changes";
  document.getElementById("cancelEdit").classList.remove("hidden");
  document.getElementById("subject").focus();
}

async function saveClass(event) {
  event.preventDefault();
  const message = document.getElementById("classMessage");
  const payload = {
    classId: Number(document.getElementById("classId").value || 0),
    subject: document.getElementById("subject").value.trim(),
    fee: Number(document.getElementById("fee").value || 0),
    location: document.getElementById("location").value.trim(),
    standard: document.getElementById("standard").value.trim(),
    rating: Number(document.getElementById("rating").value || 0),
    mode: document.getElementById("mode").value,
    imageUrl: document.getElementById("imageUrl").value.trim(),
    description: document.getElementById("description").value.trim(),
    materialProvided: document.getElementById("materialProvided").value.trim(),
    contactNumber: document.getElementById("contactNumber").value.trim(),
    classTimings: document.getElementById("classTimings").value.trim()
  };
  const response = await fetch("/api/owner/classes", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
  const result = await response.json();
  if (!response.ok) { message.textContent = result.message || "Class could not be saved."; return; }
  message.textContent = payload.classId ? "Class updated." : "Class published.";
  resetClassForm();
  await loadOwnerClasses();
}

async function uploadOwnerImage(event) {
  const file = event.target.files?.[0];
  if (!file) return;
  document.getElementById("imageFileName").textContent = file.name;
  document.getElementById("imageUploadBox").classList.add("hidden");
  document.getElementById("changeImageBtn").classList.remove("hidden");
  const preview = document.getElementById("imagePreview");
  preview.src = URL.createObjectURL(file);
  preview.classList.remove("hidden");
  const formData = new FormData();
  formData.append("image", file);
  const response = await fetch("/api/owner/images", { method: "POST", body: formData });
  const result = await response.json();
  if (!response.ok) {
    document.getElementById("classMessage").textContent = result.message || "Image upload failed.";
    event.target.value = "";
    document.getElementById("imageFileName").textContent = "No image selected";
    document.getElementById("imageUploadBox").classList.remove("hidden");
    document.getElementById("changeImageBtn").classList.add("hidden");
    preview.classList.add("hidden");
    return;
  }
  document.getElementById("imageUrl").value = result.imageUrl;
}

async function deleteClass(classId) {
  if (!window.confirm("Delete this class?")) return;
  const response = await fetch(`/api/owner/classes/${classId}`, { method: "DELETE" });
  if (response.ok) { await loadOwnerClasses(); return; }
  const result = await response.json();
  window.alert(result.message || "Class could not be deleted.");
}

function renderEnquiries(items) {
  const container = document.getElementById("ownerEnquiries");
  if (!container) return;
  container.innerHTML = items.length ? `<div class="owner-table-wrap"><table class="owner-table"><thead><tr><th>Sr. No.</th><th>Class</th><th>Student Name</th><th>Phone No.</th><th>Standard</th><th>Board</th><th>Yearly Budget</th><th>Email</th><th>Enquiry Date</th></tr></thead><tbody>${items.map((item, index) => `<tr><td>${index + 1}</td><td>${escapeHtml(item.subject || "Class")}</td><td>${escapeHtml(item.studentName || "Student")}</td><td>${escapeHtml(item.studentPhone || "Not available")}</td><td>${escapeHtml(item.studentStandard || "Not provided")}</td><td>${escapeHtml(item.studentBoard || "Not provided")}</td><td>${escapeHtml(item.yearlyBudget || "Not provided")}</td><td>${escapeHtml(item.studentEmail || "Not available")}</td><td>${item.enquiryDate ? new Date(item.enquiryDate).toLocaleDateString("en-IN") : "Not available"}</td></tr>`).join("")}</tbody></table></div>` : "<p class='empty-state'>No student enquiries for your classes yet.</p>";
}

function renderReviews(items) {
  const container = document.getElementById("ownerReviews");
  if (!container) return;
  container.innerHTML = items.length ? items.map((item) => `<article class="owner-list-item"><div><span class="owner-list-label">${escapeHtml(item.subject || "Class")}</span><h3>${escapeHtml(item.studentName || "Student")} <span class="review-stars">★ ${Number(item.rating || 0).toFixed(1)}</span></h3><p>${escapeHtml(item.comment || "No comment added.")}</p></div><div class="owner-list-side"><span>${new Date(item.createdAt).toLocaleDateString("en-IN")}</span></div></article>`).join("") : "<p class='empty-state'>No reviews for your classes yet.</p>";
}

async function loadOwnerSecondaryPage() {
  const page = document.body.dataset.ownerPage;
  if (!page) return;
  const response = await fetch(`/api/owner/${page}`);
  if (response.status === 401) { logout(); return; }
  const data = await response.json();
  page === "enquiries" ? renderEnquiries(Array.isArray(data) ? data : []) : renderReviews(Array.isArray(data) ? data : []);
}

document.addEventListener("DOMContentLoaded", async () => {
  showOwnerName();
  document.querySelectorAll("[data-logout]").forEach((button) => button.addEventListener("click", logout));
  document.getElementById("classForm")?.addEventListener("submit", saveClass);
  document.getElementById("imageFile")?.addEventListener("change", uploadOwnerImage);
  document.getElementById("changeImageBtn")?.addEventListener("click", () => document.getElementById("imageFile")?.click());
  document.getElementById("cancelEdit")?.addEventListener("click", resetClassForm);
  document.addEventListener("click", (event) => {
    const editButton = event.target.closest("[data-edit-class]");
    const deleteButton = event.target.closest("[data-delete-class]");
    if (editButton) editClass(Number(editButton.dataset.editClass));
    if (deleteButton) deleteClass(Number(deleteButton.dataset.deleteClass));
  });
  if (document.getElementById("ownerClassCards")) await loadOwnerClasses();
  await loadOwnerSecondaryPage();
});
