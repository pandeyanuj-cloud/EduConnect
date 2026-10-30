fetch("/api/classes")
  .then(response => response.json())
  .then(data => {
    const container = document.getElementById("classList");
    container.innerHTML = "";
    data.forEach(cls => {
      const card = document.createElement("div");
      card.className = "card";
      card.innerHTML = `<h3>${cls.subject}</h3><p>₹${cls.fee}/mo · ${cls.location}</p>`;
      container.appendChild(card);
    });
  })
  .catch(err => {
    document.getElementById("classList").innerHTML = "<p>Could not load classes.</p>";
    console.error(err);
  });