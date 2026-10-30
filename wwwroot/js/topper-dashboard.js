const topperState = { profile: null };

function topperEscape(value) { return String(value ?? "").replace(/[&<>"']/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;" })[character]); }
function topperUser() { try { return JSON.parse(localStorage.getItem("eduConnectUser") || "null"); } catch { return null; } }
function topperLogout() { fetch("/api/auth/logout", { method: "POST" }).finally(() => { localStorage.removeItem("eduConnectUser"); window.location.href = "login.html"; }); }
function setTopperMessage(message) { document.getElementById("topperMessage").textContent = message; }

function updateTopperFileName(fileId, urlId) {
  const file = document.getElementById(fileId);
  const name = document.getElementById(`${fileId}Name`);
  const uploadedUrl = document.getElementById(urlId)?.value;
  if (!file || !name) return;
  name.textContent = file.files?.[0]?.name || (uploadedUrl ? "File uploaded · choose to replace" : "No file selected");
  name.classList.toggle("has-file", Boolean(file.files?.[0] || uploadedUrl));
}

async function uploadTopperFile(file) {
  if (!file) return "";
  const formData = new FormData(); formData.append("file", file);
  const response = await fetch("/api/topper/files", { method: "POST", body: formData });
  const result = await response.json();
  if (!response.ok) throw new Error(result.message || "File upload failed.");
  return result.fileUrl;
}

function renderTopperCard(profile) {
  const container = document.getElementById("topperCard");
  if (!container) return;
  if (!profile) { container.innerHTML = "<p class='empty-state'>Save your profile to generate a topper card.</p>"; return; }
  container.innerHTML = `<article class="class-card owner-class-card"><img class="class-image" src="${topperEscape(profile.photoUrl || "https://placehold.co/800x450/eaf2f6/17324d?text=Topper")}" alt="${topperEscape(profile.name)}" /><div class="card-body"><div class="card-topline"><div><h3>${topperEscape(profile.name)}</h3><span>${topperEscape(profile.standard)}</span></div><span class="rating-pill">${profile.marks10 ?? "NA"}% / ${profile.marks12 ?? "NA"}%</span></div><div class="meta-tags"><span>10th: ${profile.marks10 ?? "NA"}%</span><span>12th: ${profile.marks12 ?? "NA"}%</span></div><p class="class-description">${topperEscape(profile.description || "Academic profile")}</p><div class="card-footer"><div class="action-stack"><a class="mini-btn accent" href="topper-details.html?id=${profile.topperId}">View Details</a><button class="mini-btn ghost" type="button" id="editTopperBtn">Edit</button></div></div></div></article>`;
  document.getElementById("editTopperBtn")?.addEventListener("click", () => populateTopperForm(profile));
}

function populateTopperForm(profile) {
  topperState.profile = profile;
  if (!profile) return;
  ["name", "standard", "marks10", "marks12", "photoUrl", "marksheet10Url", "marksheet12Url", "description", "pricePerDay", "pricePerMonth", "pricePerYear"].forEach((id) => { const element = document.getElementById(id); if (element) element.value = profile[id] ?? ""; });
  [["photoFile", "photoUrl"], ["marksheet10File", "marksheet10Url"], ["marksheet12File", "marksheet12Url"]].forEach(([fileId, urlId]) => updateTopperFileName(fileId, urlId));
  document.getElementById("formTitle").textContent = "Edit your profile"; document.getElementById("formEyebrow").textContent = "Update profile"; document.getElementById("cancelEdit").classList.remove("hidden");
}

async function loadTopperProfile() {
  const response = await fetch("/api/topper/profile");
  if (response.status === 401) { topperLogout(); return; }
  if (response.status === 200) { topperState.profile = await response.json(); populateTopperForm(topperState.profile); renderTopperCard(topperState.profile); }
  else renderTopperCard(null);
}

async function saveTopperProfile(event) {
  event.preventDefault();
  try {
    const profile = { topperId: topperState.profile?.topperId || 0, name: document.getElementById("name").value.trim(), standard: document.getElementById("standard").value.trim(), marks10: Number(document.getElementById("marks10").value) || null, marks12: Number(document.getElementById("marks12").value) || null, marksheet10Url: document.getElementById("marksheet10Url").value, marksheet12Url: document.getElementById("marksheet12Url").value, photoUrl: document.getElementById("photoUrl").value, description: document.getElementById("description").value.trim(), pricePerDay: Number(document.getElementById("pricePerDay").value) || null, pricePerMonth: Number(document.getElementById("pricePerMonth").value) || null, pricePerYear: Number(document.getElementById("pricePerYear").value) || null };
    const fileMappings = [["photoFile", "photoUrl"], ["marksheet10File", "marksheet10Url"], ["marksheet12File", "marksheet12Url"]];
    for (const [fileId, urlId] of fileMappings) { const file = document.getElementById(fileId).files?.[0]; if (file) document.getElementById(urlId).value = profile[urlId] = await uploadTopperFile(file); }
    const response = await fetch("/api/topper/profile", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(profile) });
    const result = await response.json(); if (!response.ok) throw new Error(result.message || "Profile could not be saved.");
    topperState.profile = result; renderTopperCard(result); populateTopperForm(result); setTopperMessage("Profile saved successfully.");
  } catch (error) { setTopperMessage(error.message); }
}

function renderTopperEnquiries(items) { const container = document.getElementById("topperEnquiries"); if (!container) return; container.innerHTML = items.length ? `<div class="owner-table-wrap"><table class="owner-table"><thead><tr><th>Sr. No.</th><th>Student Name</th><th>Phone No.</th><th>Email</th><th>Standard</th></tr></thead><tbody>${items.map((item, index) => `<tr><td>${index + 1}</td><td>${topperEscape(item.studentName)}</td><td>${topperEscape(item.studentPhone)}</td><td>${topperEscape(item.studentEmail)}</td><td>${topperEscape(item.studentStandard)}</td></tr>`).join("")}</tbody></table></div>` : "<p class='empty-state'>No student enquiries yet.</p>"; }

async function loadTopperEnquiries() { const response = await fetch("/api/topper/enquiries"); if (response.status === 401) return topperLogout(); renderTopperEnquiries(response.ok ? await response.json() : []); }
document.addEventListener("DOMContentLoaded", async () => { const user = topperUser(); if (!user) return topperLogout(); document.getElementById("topperName")?.replaceChildren(document.createTextNode(user.fullName || "Topper")); document.querySelectorAll("[data-logout]").forEach((button) => button.addEventListener("click", topperLogout)); document.getElementById("topperForm")?.addEventListener("submit", saveTopperProfile); [["photoFile", "photoUrl"], ["marksheet10File", "marksheet10Url"], ["marksheet12File", "marksheet12Url"]].forEach(([fileId, urlId]) => { const input = document.getElementById(fileId); input?.addEventListener("change", () => updateTopperFileName(fileId, urlId)); updateTopperFileName(fileId, urlId); }); document.getElementById("cancelEdit")?.addEventListener("click", () => { document.getElementById("topperForm").reset(); populateTopperForm(topperState.profile); }); if (document.getElementById("topperForm")) await loadTopperProfile(); if (document.getElementById("topperEnquiries")) await loadTopperEnquiries(); });