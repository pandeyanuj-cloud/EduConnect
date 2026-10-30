const STORAGE_KEY = "eduConnectStudentProfile";
const PROFILE_SECTION = "profile";

const state = {
  currentUser: null,
  profile: null,
  classes: [],
  toppers: [],
  savedClasses: [],
  bookings: [],
  aiHistory: [],
  activeSection: "dashboard",
  selectedClassId: null
};

function getCurrentUser() {
  const raw = localStorage.getItem("eduConnectUser");
  try {
    state.currentUser = raw ? JSON.parse(raw) : null;
  } catch (error) {
    state.currentUser = null;
  }

  return state.currentUser;
}

function getStoredProfile() {
  const saved = localStorage.getItem(STORAGE_KEY);
  if (saved) {
    try {
      return JSON.parse(saved);
    } catch (error) {
      return null;
    }
  }
  return null;
}

function saveStoredProfile(profile) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(profile));
}

function getSelectedSubjectsFromForm() {
  const selected = Array.from(document.querySelectorAll('input[name="subjects"]:checked'));
  return selected.map((item) => item.value);
}

function getSubjectInputValue() {
  const value = document.getElementById("subjectsInput")?.value || "";
  return value.split(",").map((item) => item.trim()).filter(Boolean);
}

function calculateCompletion(profile) {
  if (!profile) return 0;

  const hasBudget = Number(profile.budget) > 0 ||
    Boolean(profile.preferredPriceRange && (
      profile.preferredPriceRange !== "Custom" || Number(profile.customPrice) > 0
    ));
  const requiredFields = [
    profile.studentName,
    profile.location,
    profile.standard,
    hasBudget,
    profile.learningMode,
    (profile.subjects || []).length > 0
  ];

  const filled = requiredFields.filter(Boolean).length;
  return Math.min(Math.round((filled / 6) * 100), 100);
}

function formatMoney(value) {
  const amount = Number(value || 0);
  return `₹${amount.toLocaleString("en-IN")}`;
}

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, (character) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#039;"
  })[character]);
}

function getYearlyBudget(profile) {
  return profile?.preferredPriceRange || (profile?.customPrice ? `${formatMoney(profile.customPrice)}/year` : "Not added");
}

function sectionNameToId(section) {
  return section || "dashboard";
}

function setActiveSection(section) {
  state.activeSection = sectionNameToId(section);

  document.querySelectorAll(".view-section").forEach((el) => {
    el.classList.toggle("hidden", el.id !== state.activeSection);
  });

  document.querySelectorAll(".nav-item").forEach((el) => {
    el.classList.toggle("active", el.dataset.section === state.activeSection);
  });

  document.querySelectorAll(".action-btn").forEach((el) => {
    const target = el.dataset.section;
    if (target) el.classList.toggle("active", target === state.activeSection);
  });
}

function buildProfilePayload() {
  const selectedSubjects = getSelectedSubjectsFromForm();
  const subjectList = selectedSubjects.length ? selectedSubjects : getSubjectInputValue();

  return {
    studentName: document.getElementById("studentName").value.trim(),
    mobile: document.getElementById("mobile").value.trim(),
    dateOfBirth: document.getElementById("dateOfBirth").value || null,
    gender: document.getElementById("gender").value,
    profilePhoto: document.getElementById("profilePhoto").value.trim() || document.getElementById("profilePhotoPreview").src,
    location: document.getElementById("location").value.trim(),
    standard: document.getElementById("standard").value.trim(),
    board: document.getElementById("board").value,
    schoolName: document.getElementById("schoolName").value.trim(),
    latitude: state.location?.latitude || null,
    longitude: state.location?.longitude || null,
    budget: Number(document.getElementById("budget")?.value || 0),
    preferredPriceRange: document.getElementById("preferredPriceRange").value,
    customPrice: Number(document.getElementById("customPrice").value || 0) || null,
    learningMode: document.querySelector('input[name="learningMode"]:checked')?.value || "Both",
    subjects: subjectList.join(",")
  };
}

function populateForm(profile) {
  const safeProfile = profile || {
    studentName: "",
    mobile: "",
    dateOfBirth: "",
    gender: "",
    profilePhoto: "",
    location: "",
    standard: "",
    board: "",
    schoolName: "",
    budget: 0,
    preferredPriceRange: "",
    customPrice: null,
    learningMode: "Both",
    subjects: []
  };

  document.getElementById("studentName").value = safeProfile.studentName || "";
  document.getElementById("mobile").value = safeProfile.mobile || "";
  document.getElementById("dateOfBirth").value = safeProfile.dateOfBirth ? String(safeProfile.dateOfBirth).slice(0, 10) : "";
  document.getElementById("gender").value = safeProfile.gender || "";
  document.getElementById("profilePhoto").value = safeProfile.profilePhoto || "";
  document.getElementById("profilePhotoPreview").src = safeProfile.profilePhoto || "https://placehold.co/120x120/edf2f7/1f3c7a?text=Photo";
  document.getElementById("location").value = safeProfile.location || "";
  document.getElementById("standard").value = safeProfile.standard || "";
  document.getElementById("board").value = safeProfile.board || "";
  document.getElementById("schoolName").value = safeProfile.schoolName || "";
  document.getElementById("preferredPriceRange").value = safeProfile.preferredPriceRange || "";
  document.getElementById("customPrice").value = safeProfile.customPrice || "";
  document.getElementById("customPriceRow").classList.toggle("hidden", safeProfile.preferredPriceRange !== "Custom");
  document.getElementById("subjectsInput").value = (safeProfile.subjects || []).join(", ");

  document.querySelectorAll('input[name="learningMode"]').forEach((el) => {
    el.checked = el.value === (safeProfile.learningMode || "Both");
  });

  document.querySelectorAll('input[name="subjects"]').forEach((box) => {
    box.checked = (safeProfile.subjects || []).includes(box.value);
  });
}

function setProfileEditMode(editing) {
  document.getElementById("profileDisplay").classList.toggle("hidden", editing);
  document.getElementById("studentProfileForm").classList.toggle("hidden", !editing);
  document.getElementById("toggleEditProfileBtn").classList.toggle("hidden", editing);
}

function displayValue(value) {
  return value || "Not added";
}

async function reverseGeocode(latitude, longitude) {
  try {
    const response = await fetch(`https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${latitude}&lon=${longitude}`);
    if (!response.ok) return "";

    const result = await response.json();
    return result.display_name || "";
  } catch (error) {
    console.error("Could not resolve the selected location.", error);
    return "";
  }
}

function isCoordinateLocation(value) {
  return typeof value === "string" && /^Lat\s-?\d+(\.\d+)?,\sLong\s-?\d+(\.\d+)?$/i.test(value.trim());
}

function readableLocation(value) {
  return isCoordinateLocation(value) ? "Selected location" : displayValue(value);
}

function renderProfileDisplay(profile) {
  const user = getCurrentUser();
  const name = profile?.studentName || user?.fullName || "Complete your profile";
  document.getElementById("displayStudentName").textContent = name;
  document.getElementById("displayEmail").textContent = profile?.email || user?.email || "Email not available";
  document.getElementById("profileInitials").textContent = name.split(" ").map((part) => part[0]).join("").slice(0, 2).toUpperCase();
  document.getElementById("displayMobile").textContent = displayValue(profile?.mobile);
  document.getElementById("displayDateOfBirth").textContent = profile?.dateOfBirth ? new Date(profile.dateOfBirth).toLocaleDateString("en-IN") : "Not added";
  document.getElementById("displayGender").textContent = displayValue(profile?.gender);
  document.getElementById("displayStandard").textContent = displayValue(profile?.standard);
  document.getElementById("displayBoard").textContent = displayValue(profile?.board);
  document.getElementById("displaySchoolName").textContent = displayValue(profile?.schoolName);
  document.getElementById("displayLocation").textContent = readableLocation(profile?.location);
  document.getElementById("displayBudget").textContent = displayValue(profile?.preferredPriceRange || (profile?.customPrice ? `₹${Number(profile.customPrice).toLocaleString("en-IN")}/year` : ""));
  document.getElementById("displayLearningMode").textContent = displayValue(profile?.learningMode);
  document.getElementById("profileEmptyMessage").classList.toggle("hidden", Boolean(profile?.studentName));
}

function renderProfileSummary(profile) {
  const user = getCurrentUser();
  const name = profile?.studentName || user?.fullName || "Student";
  const location = readableLocation(profile?.location);
  const standard = profile?.standard || "your standard";
  const subjects = (profile?.subjects || []).length ? profile.subjects.join(", ") : "Not selected";

  document.getElementById("welcomeTitle").textContent = `Welcome, ${name}`;
  document.getElementById("userChip").textContent = `${location} • ${standard}`;
  document.getElementById("locationText").textContent = location;
  document.getElementById("standardText").textContent = standard;
  document.getElementById("subjectsText").textContent = subjects;
  document.getElementById("budgetText").textContent = getYearlyBudget(profile);
  document.getElementById("modeText").textContent = profile?.learningMode || "Both";

  const completion = calculateCompletion(profile);
  document.getElementById("completionText").textContent = `${completion}%`;
  document.getElementById("completionBar").style.width = `${completion}%`;

  const photoUrl = profile?.profilePhoto || "https://placehold.co/120x120/edf2f7/1f3c7a?text=Photo";
  document.getElementById("profilePhotoPreview").src = photoUrl;
}

function renderProfileCard(profile) {
  const card = document.getElementById("profileCard");
  if (!profile || !profile.studentName) {
    card.innerHTML = "<p class='empty-state'>No profile saved yet. Fill in your details and submit the profile.</p>";
    return;
  }

  card.innerHTML = `
    <div class="profile-card-top">
      <img src="${profile.profilePhoto || 'https://placehold.co/120x120/edf2f7/1f3c7a?text=Photo'}" alt="Profile photo" />
      <div>
        <h3>${profile.studentName}</h3>
        <p>${profile.location || "Location not added"}</p>
      </div>
    </div>
    <div class="profile-info-grid">
      <div><span>Standard</span><strong>${profile.standard || "Not set"}</strong></div>
      <div><span>Budget</span><strong>${getYearlyBudget(profile)}</strong></div>
      <div><span>Mode</span><strong>${profile.learningMode || "Both"}</strong></div>
      <div><span>Subjects</span><strong>${(profile.subjects || []).join(", ") || "Not selected"}</strong></div>
    </div>
  `;
}

function openClassModal(classItem) {
  const modal = document.getElementById("classDetailModal");
  const content = document.getElementById("classDetailContent");

  content.innerHTML = `
    <div class="detail-layout">
      <img src="${escapeHtml(classItem.imageUrl || 'https://placehold.co/600x300/edf2f7/1f3c7a?text=Class')}" alt="${escapeHtml(classItem.subject)}" />
      <div>
        <h2>${escapeHtml(classItem.subject || 'Class')}</h2>
        <p class="pill-row"><span class="pill">${escapeHtml(classItem.location || 'Location not provided')}</span> <span class="pill">${escapeHtml(classItem.standard || 'All levels')}</span> <span class="pill">★ ${Number(classItem.rating || 0).toFixed(1)}</span></p>
        <p>${escapeHtml(classItem.fullDescription || classItem.description || 'No detailed description is available for this class.')}</p>
        <ul class="detail-list">
          <li><strong>Fees:</strong> ${formatMoney(classItem.fee || 0)}</li>
          <li><strong>Mode:</strong> ${escapeHtml(classItem.mode || 'Both')}</li>
          <li><strong>Contact number:</strong> ${escapeHtml(classItem.contactNumber || 'Not provided')}</li>
          <li><strong>Timings:</strong> ${escapeHtml(classItem.classTimings || 'Not provided')}</li>
          <li><strong>Material:</strong> ${escapeHtml(classItem.materialProvided || classItem.studyMaterial || 'Not provided')}</li>
        </ul>
        <div class="modal-actions">
          <button class="btn accent" type="button" data-book-class="${classItem.classId}">Contact Class</button>
        </div>
      </div>
    </div>
  `;

  modal.classList.remove("hidden");
}

function renderClassCards(items, containerId, options = {}) {
  const container = document.getElementById(containerId);
  if (!container) return;

  if (!items || !items.length) {
    container.innerHTML = `<p class='empty-state'>${options.emptyMessage || 'No classes found matching your requirements.'}</p>`;
    return;
  }

  const savedClassIds = state.savedClasses.map((item) => item.classId);

  container.innerHTML = items.map((item) => `
    <article class="class-card">
      <img class="class-image" src="${item.imageUrl || 'https://placehold.co/600x300/edf2f7/1f3c7a?text=Class'}" alt="${item.subject || 'Class'}" />
      <div class="card-body">
        <div class="card-topline">
          <div>
            <h3>${item.subject || 'Class'}</h3>
            <span>${item.location || 'Location not provided'}</span>
          </div>
          <span class="rating-pill">★ ${Number(item.rating || 0).toFixed(1)}</span>
        </div>
        <div class="meta-tags">
          <span>${item.standard || 'All levels'}</span>
          <span>${item.mode || 'Both'}</span>
        </div>
        <p class="class-description">${item.description || item.fullDescription || 'No description available yet.'}</p>${item.materialProvided ? `<p class="class-material"><strong>Material:</strong> ${item.materialProvided}</p>` : ''}
        <div class="card-footer">
          <div class="price-block">
            <strong>${formatMoney(item.fee || 0)}</strong>
            <span>/ Year</span>
          </div>
          <div class="action-stack">
            <button type="button" class="mini-btn ghost" data-view-class="${item.classId}">View Details</button>
            <button type="button" class="mini-btn accent" data-book-class="${item.classId}">Contact Class</button>
            ${options.savedView ? `<button type="button" class="mini-btn ghost" data-remove-saved="${item.classId}">Remove</button>` : `<button type="button" class="mini-btn ${savedClassIds.includes(item.classId) ? 'saved' : 'ghost'}" data-save-class="${savedClassIds.includes(item.classId) ? '' : item.classId}" ${savedClassIds.includes(item.classId) ? 'disabled' : ''}>${savedClassIds.includes(item.classId) ? 'Saved' : 'Save'}</button>`}
          </div>
        </div>
      </div>
    </article>
  `).join("");
}

function renderTopperCards(toppers) {
  const container = document.getElementById("topperResults");
  if (!toppers || !toppers.length) {
    container.innerHTML = "<p class='empty-state'>No toppers found.</p>";
    return;
  }

  container.innerHTML = toppers.map((topper) => `
    <article class="class-card topper-card">
      <img class="class-image" src="${topper.photoUrl || 'https://placehold.co/600x300/edf2f7/1f3c7a?text=Topper'}" alt="${topper.name}" />
      <div class="card-body">
        <div class="card-topline">
          <div>
            <h3>${topper.name}</h3>
            <span>${topper.standard || 'Standard not provided'}</span>
          </div>
          <span class="rating-pill">${topper.marks10 ?? 'NA'}% / ${topper.marks12 ?? 'NA'}%</span>
        </div>
        <div class="meta-tags">
          <span>10th: ${topper.marks10 ?? 'NA'}%</span>
          <span>12th: ${topper.marks12 ?? 'NA'}%</span>
        </div>
        <p class="class-description">${topper.description || 'Top performer with strong academic focus.'}</p>
        <div class="card-footer">
          <div class="action-stack">
            <a class="mini-btn accent" href="topper-details.html?id=${topper.topperId}">View Details</a>
          </div>
        </div>
      </div>
    </article>
  `).join("");
}

function renderBookings(bookings) {
  const container = document.getElementById("bookingsList");
  if (!bookings || !bookings.length) {
    container.innerHTML = "<p class='empty-state'>No bookings yet. Book a class or topper to see it here.</p>";
    return;
  }

  container.innerHTML = bookings.map((booking) => `
    <div class="booking-item">
      <div>
        <strong>${booking.targetName || 'Item'}</strong>
        <p>${booking.type || 'Class'} • ${new Date(booking.bookingDate).toLocaleDateString()}</p>
      </div>
      <div>
        <span class="booking-status">${booking.status || 'Pending'}</span>
      </div>
      <div>
        <small>${booking.contactInfo || 'Contact will be shared soon.'}</small>
      </div>
    </div>
  `).join("");
}

function renderSavedClasses(saved) {
  renderClassCards(saved, "savedClassesList", { savedView: true, emptyMessage: "No saved classes yet." });
}

async function loadProfile() {
  const user = getCurrentUser();
  if (!user?.userId) {
    return;
  }

  try {
    const response = await fetch("/api/student/profile");
    if (response.status === 404) {
      state.profile = null;
      populateForm(null);
      renderProfileDisplay(null);
      setProfileEditMode(true);
      return;
    }
    if (!response.ok) throw new Error("Unable to load profile");
    const profile = await response.json();
    if (profile && profile.userId) {
      state.profile = {
        ...profile,
        subjects: profile.subjects ? String(profile.subjects).split(",").map((s) => s.trim()).filter(Boolean) : []
      };
      if (isCoordinateLocation(state.profile.location) && state.profile.latitude != null && state.profile.longitude != null) {
        state.profile.location = await reverseGeocode(state.profile.latitude, state.profile.longitude) || "Selected location";
      }
      saveStoredProfile(state.profile);
      populateForm(state.profile);
      renderProfileSummary(state.profile);
      renderProfileCard(state.profile);
      renderProfileDisplay(state.profile);
      setProfileEditMode(false);
    }
    else {
      state.profile = null;
      populateForm(null);
      renderProfileDisplay(null);
      setProfileEditMode(true);
    }
  } catch (error) {
    console.error("Could not load the saved student profile.", error);
    document.getElementById("profileMessage").textContent = "Your saved profile could not be loaded. Please refresh the page or try again later.";
  }
}

async function saveProfileToServer() {
  const user = getCurrentUser();
  if (!user?.userId) {
    document.getElementById("profileMessage").textContent = "Please log in before saving the profile.";
    return;
  }

  const payload = buildProfilePayload();
  if (!payload.studentName || !payload.location || !payload.standard) {
    document.getElementById("profileMessage").textContent = "Please complete your name, location, and standard before saving.";
    return;
  }

  const response = await fetch("/api/student/profile", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  const result = await response.json();
  if (!response.ok) {
    document.getElementById("profileMessage").textContent = result.message || "Profile could not be saved.";
    return;
  }

  state.profile = result.profile || payload;
  state.profile.subjects = Array.isArray(result.profile?.subjects)
    ? result.profile.subjects
    : String(result.profile?.subjects || payload.subjects || "").split(",").map((s) => s.trim()).filter(Boolean);

  saveStoredProfile(state.profile);
  populateForm(state.profile);
  renderProfileSummary(state.profile);
  renderProfileCard(state.profile);
  renderProfileDisplay(state.profile);
  setProfileEditMode(false);
  renderRecommendations(state.profile);
  renderRecentlyViewed();
  document.getElementById("profileMessage").textContent = "Profile saved successfully.";
}

async function loadToppers() {
  const response = await fetch("/api/student/toppers");
  state.toppers = await response.json();
  renderTopperCards(state.toppers);
}

async function loadBookings() {
  const user = getCurrentUser();
  if (!user?.userId) {
    renderBookings([]);
    return;
  }

  const response = await fetch(`/api/student/bookings?userId=${user.userId}`);
  const data = await response.json();
  state.bookings = Array.isArray(data) ? data : [];
  renderBookings(state.bookings);
}

async function loadSavedClasses() {
  const user = getCurrentUser();
  if (!user?.userId) {
    renderSavedClasses([]);
    return;
  }

  const response = await fetch(`/api/student/saved-classes?userId=${user.userId}`);
  const data = await response.json();
  state.savedClasses = Array.isArray(data) ? data : [];
  renderSavedClasses(state.savedClasses);

  if (state.classes && state.classes.length) {
    renderClassCards(state.classes, "classSearchResults");
  }
}

async function applyClassSearch(event) {
  event.preventDefault();

  const results = document.getElementById("classSearchResults");
  if (results) results.innerHTML = "";

  const filters = {
    location: document.getElementById("searchLocation")?.value || "",
    standard: document.getElementById("searchStandard")?.value || "",
    subject: document.getElementById("searchSubject")?.value || "",
    minBudget: document.getElementById("searchMinBudget")?.value || "",
    budget: document.getElementById("searchBudget")?.value || "",
    mode: document.getElementById("searchMode")?.value || "All",
    rating: document.getElementById("searchRating")?.value || ""
  };

  try {
    const query = new URLSearchParams(filters);
    const response = await fetch(`/api/student/classes?${query.toString()}`);
    const data = await response.json();
    state.classes = response.ok && Array.isArray(data) ? data : [];
    renderClassCards(state.classes, "classSearchResults");
  } catch (error) {
    state.classes = [];
    renderClassCards(state.classes, "classSearchResults");
    console.error("Could not load filtered classes.", error);
  }
}

async function handleClassAction(event) {
  const target = event.target.closest("[data-view-class], [data-book-class], [data-save-class], [data-remove-saved]");
  if (!target) return;
  const classId = Number(target.dataset.viewClass || target.dataset.bookClass || target.dataset.saveClass || target.dataset.removeSaved);
  if (!classId && classId !== 0) return;

  if (target.dataset.viewClass) {
    const item = state.classes.find((classItem) => classItem.classId === classId) || await fetchClassById(classId);
    if (item) openClassModal(item);
  }

  if (target.dataset.bookClass) {
    const user = getCurrentUser();
    if (!user?.userId) {
      alert("Please log in to contact a class.");
      return;
    }
    const classItem = state.classes.find((item) => item.classId === classId) || await fetchClassById(classId);
    if (!classItem) return;

    const payload = {
      targetId: classId,
      targetName: classItem.subject || "Class"
    };

    try {
      const response = await fetch("/api/student/bookings", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });
      const result = await response.json();
      if (!response.ok) {
        alert(result.message || "Could not contact this class.");
        return;
      }

      await loadBookings();
      alert("Your enquiry has been sent. The class will contact you shortly.");
    } catch (error) {
      console.error("Could not send the class enquiry.", error);
      alert("Could not contact this class. Please try again.");
    }
  }

  if (target.dataset.saveClass) {
    const user = getCurrentUser();
    if (!user?.userId) {
      alert("Please log in to save a class.");
      return;
    }

    const response = await fetch(`/api/student/saved-classes?userId=${user.userId}&classId=${classId}`, { method: "POST" });
    if (response.ok) {
      await loadSavedClasses();
      renderClassCards(state.classes, "classSearchResults");
    } else {
      let result = {};
      try { result = await response.json(); } catch (error) { }
      alert(result.message || "The class could not be saved. Please check that the SavedClasses table exists.");
    }
  }

  if (target.dataset.removeSaved) {
    const user = getCurrentUser();
    const response = await fetch(`/api/student/saved-classes?userId=${user?.userId || 0}&classId=${classId}`, { method: "DELETE" });
    if (response.ok) {
      await loadSavedClasses();
      renderClassCards(state.classes, "classSearchResults");
    } else {
      alert("The saved class could not be removed.");
    }
  }
}

async function fetchClassById(classId) {
  const response = await fetch(`/api/student/classes?classId=${classId}`);
  const data = await response.json();
  return Array.isArray(data) && data.length ? data[0] : null;
}

async function askAI() {
  const inputElement = document.getElementById("aiQuestion");
  const askButton = document.getElementById("askAiBtn");
  const input = inputElement.value.trim();
  if (!input) return;

  const chatBox = document.getElementById("aiChatBox");
  const appendMessage = (role, content) => {
    const message = document.createElement("div");
    message.className = `chat-message ${role}`;
    message.textContent = content;
    chatBox.appendChild(message);
    chatBox.scrollTop = chatBox.scrollHeight;
    return message;
  };

  appendMessage("user", input);
  inputElement.value = "";
  askButton.disabled = true;
  const pendingMessage = appendMessage("assistant", "Thinking…");

  try {
    const response = await fetch("/api/student/ai-helper", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question: input, history: state.aiHistory.slice(-8) })
    });
    const result = await response.json();
    if (!response.ok) throw new Error(result.message || "The AI Helper could not answer right now.");

    pendingMessage.textContent = result.answer;
    state.aiHistory.push({ role: "user", content: input }, { role: "assistant", content: result.answer });
    state.aiHistory = state.aiHistory.slice(-8);
  } catch (error) {
    pendingMessage.textContent = error.message || "The AI Helper is temporarily unavailable. Please try again.";
    console.error("AI Helper request failed.", error);
  } finally {
    askButton.disabled = false;
    inputElement.focus();
    chatBox.scrollTop = chatBox.scrollHeight;
  }
}

function initLocationButton() {
  document.getElementById("detectLocationBtn")?.addEventListener("click", () => {
    if (!navigator.geolocation) {
      document.getElementById("profileMessage").textContent = "Geolocation is not supported by this browser.";
      return;
    }

    navigator.geolocation.getCurrentPosition(
      async (position) => {
        state.location = { latitude: position.coords.latitude, longitude: position.coords.longitude };
        const address = await reverseGeocode(position.coords.latitude, position.coords.longitude);
        document.getElementById("location").value = address || "Selected location";
        document.getElementById("profileMessage").textContent = "Location captured. You can adjust or keep it before saving.";
      },
      () => {
        document.getElementById("profileMessage").textContent = "Location permission was denied. You may enter your location manually.";
      }
    );
  });
}

function initLiveLocation() {
  document.getElementById("useLiveLocationBtn")?.addEventListener("click", () => {
    const locationInput = document.getElementById("searchLocation");
    if (!navigator.geolocation) {
      document.getElementById("profileMessage").textContent = "Live location is not supported by this browser.";
      return;
    }

    navigator.geolocation.getCurrentPosition(async (position) => {
      let locality = "";
      try {
        const response = await fetch(`https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${position.coords.latitude}&lon=${position.coords.longitude}`);
        const result = await response.json();
        const address = result.address || {};
        locality = address.suburb || address.town || address.city || address.village || address.municipality || "";
      } catch (error) {
        console.error("Could not resolve live location.", error);
      }
      if (locality) locationInput.value = locality;
    }, () => {
      document.getElementById("profileMessage").textContent = "Location permission was denied. You can enter a location manually.";
    });
  });
}

function initPhotoUpload() {
  const fileInput = document.getElementById("profilePhotoFile");
  const preview = document.getElementById("profilePhotoPreview");

  fileInput.addEventListener("change", (event) => {
    const file = event.target.files && event.target.files[0];
    if (!file) return;

    const validTypes = ["image/jpeg", "image/png", "image/webp"];
    if (!validTypes.includes(file.type)) {
      document.getElementById("profileMessage").textContent = "Please upload a JPG, PNG, or WEBP image.";
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      document.getElementById("profileMessage").textContent = "Image is too large. Please upload a file under 2MB.";
      return;
    }

    const reader = new FileReader();
    reader.onload = function () {
      const result = reader.result;
      preview.src = result;
      document.getElementById("profilePhoto").value = result;
      document.getElementById("profileMessage").textContent = "Image selected and ready to save.";
    };
    reader.readAsDataURL(file);
  });
}

async function initApp() {
  document.addEventListener("click", async (event) => {
    if (event.target.closest("[data-view-class], [data-book-class], [data-save-class], [data-remove-saved]")) {
      await handleClassAction(event);
    }
  });

  const user = getCurrentUser();
  if (!user) {
    document.getElementById("profileMessage").textContent = "Please log in to access the dashboard.";
    return;
  }

  setActiveSection("profile");
  initLocationButton();
  initLiveLocation();
  initPhotoUpload();

  document.querySelectorAll(".nav-item").forEach((button) => {
    button.addEventListener("click", (event) => {
      setActiveSection(event.currentTarget.dataset.section);
    });
  });

  document.querySelectorAll(".action-btn").forEach((button) => {
    button.addEventListener("click", (event) => {
      const section = event.currentTarget.dataset.section;
      if (section) setActiveSection(section);
    });
  });

  document.getElementById("editProfileBtn").addEventListener("click", () => { setActiveSection("profile"); setProfileEditMode(true); });
  document.getElementById("toggleEditProfileBtn").addEventListener("click", () => setProfileEditMode(true));
  document.getElementById("cancelProfileBtn").addEventListener("click", () => { populateForm(state.profile); setProfileEditMode(false); });
  document.getElementById("preferredPriceRange").addEventListener("change", (event) => {
    document.getElementById("customPriceRow").classList.toggle("hidden", event.target.value !== "Custom");
  });
  document.getElementById("studentProfileForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    await saveProfileToServer();
  });

  document.getElementById("classSearchForm").addEventListener("submit", applyClassSearch);
  document.getElementById("askAiBtn").addEventListener("click", askAI);
  document.getElementById("aiQuestion").addEventListener("keydown", (event) => {
    if (event.key === "Enter") { event.preventDefault(); askAI(); }
  });

  document.getElementById("closeModalBtn").addEventListener("click", () => {
    document.getElementById("classDetailModal").classList.add("hidden");
  });

  await loadProfile();
  await loadToppers();
  await loadSavedClasses();
  await loadBookings();

}

document.addEventListener("DOMContentLoaded", initApp);
