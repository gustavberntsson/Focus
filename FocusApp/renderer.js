// API endpoints (local setup)
const TIMER_API = 'http://localhost:5004/api';
const SESSION_API = 'http://localhost:5179/api';
const SOUND_API = 'http://localhost:5014/api';

let timerInterval = null;
let currentSound = null;
let isPaused = false;

// just refreshes UI with timer status
async function updateTimerDisplay() {
  try {
    const response = await fetch(`${TIMER_API}/status`);
    const data = await response.json();

    // nothing running -> reset UI
    if (data.status === 'idle') {
      document.getElementById('timerDisplay').textContent = '25:00';
      document.getElementById('timerType').textContent = 'READY';
      document.getElementById('status').textContent = '-';
      document.getElementById('pauseBtn').disabled = true;
      document.getElementById('stopBtn').disabled = true;
      clearInterval(timerInterval);
      return;
    }

    const minutes = Math.floor(data.secondsLeft / 60);
    const seconds = data.secondsLeft % 60;
    document.getElementById('timerDisplay').textContent =
      `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;

    let statusText = 'Paused';
    if (data.timer.status === 'running') {
      if (data.timer.type === 'pomodoro') statusText = 'Pomodoro - Active';
      else if (data.timer.type === 'break') statusText = 'Break';
      else if (data.timer.type === 'deepwork') statusText = 'Deep work - Active';
    }
    document.getElementById('status').textContent = statusText;

    document.getElementById('pauseBtn').disabled = false;
    document.getElementById('stopBtn').disabled = false;

    // timer done -> log + refresh stats + stop
    if (data.secondsLeft <= 0) {
      clearInterval(timerInterval);
      await logSession(data.timer);
      await loadStats();
      alert('Timer completed! 🎉');
      await stopTimer();
    }
  } catch (err) {
    console.error('Error updating timer:', err);
    document.getElementById('status').textContent = 'Error connecting to Timer Service';
  }
}

// start button handler
async function startTimer(type) {
  try {
    const response = await fetch(`${TIMER_API}/start`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ type })
    });

    if (response.ok) {
      isPaused = false;
      document.getElementById('pauseBtn').textContent = 'Pause';
      timerInterval = setInterval(updateTimerDisplay, 1000);
      updateTimerDisplay();
    }
  } catch (err) {
    console.error('Error starting timer:', err);
    document.getElementById('status').textContent = 'Error: Timer Service not running';
  }
}

// pause/resume toggle
async function pauseTimer() {
  try {
    const endpoint = isPaused ? 'resume' : 'pause';
    const response = await fetch(`${TIMER_API}/${endpoint}`, { method: 'POST' });

    if (response.ok) {
      isPaused = !isPaused;
      document.getElementById('pauseBtn').textContent = isPaused ? 'Resume' : 'Pause';

      if (isPaused) clearInterval(timerInterval);
      else timerInterval = setInterval(updateTimerDisplay, 1000);

      updateTimerDisplay();
    }
  } catch (err) {
    console.error('Error pausing timer:', err);
  }
}

// stop button handler
async function stopTimer() {
  try {
    await fetch(`${TIMER_API}/stop`, { method: 'POST' });
    clearInterval(timerInterval);
    isPaused = false;
    document.getElementById('pauseBtn').textContent = 'Pause';
    updateTimerDisplay();
  } catch (err) {
    console.error('Error stopping timer:', err);
  }
}

// play/stop sound
async function toggleSound(soundId) {
  try {
    const buttons = document.querySelectorAll('.sound-btn');

    if (currentSound === soundId) {
      await fetch(`${SOUND_API}/sounds/stop`, { method: 'POST' });
      buttons.forEach(b => b.classList.remove('active'));
      currentSound = null;
    } else {
      await fetch(`${SOUND_API}/sounds/play/${soundId}`, { method: 'POST' });
      buttons.forEach(b => b.classList.remove('active'));
      document.querySelector(`button[onclick="toggleSound('${soundId}')"]`).classList.add('active');
      currentSound = soundId;
    }
  } catch (err) {
    console.error('Error toggling sound:', err);
    document.getElementById('status').textContent = 'Error: Sound Service not running';
  }
}

// send finished session to log service
async function logSession(timer) {
  try {
    await fetch(`${SESSION_API}/sessions`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        type: timer.type,
        minutes: timer.minutes,
        startedAt: timer.startedAt,
        completedAt: new Date().toISOString(),
        wasCompleted: true,
        notes: null
      })
    });
  } catch (err) {
    console.error('Error logging session:', err);
  }
}

// refresh stats text
async function loadStats() {
  try {
    const response = await fetch(`${SESSION_API}/stats`);
    const data = await response.json();

    document.getElementById('todayStats').textContent =
      `${data.today.sessions} sessions (${data.today.minutes}m)`;
    document.getElementById('weekStats').textContent =
      `${data.week.sessions} sessions (${data.week.minutes}m)`;
  } catch (err) {
    console.error('Error loading stats:', err);
  }
}

// init
window.addEventListener('DOMContentLoaded', () => {
  updateTimerDisplay();
  loadStats();
});