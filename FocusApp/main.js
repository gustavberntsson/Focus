const { app, BrowserWindow } = require('electron');
const { spawn } = require('child_process');
const path = require('path');

let mainWindow;
let backendProcesses = [];

function startBackendServices() {
  const services = [
    { name: 'TimerService', path: '../TimerService', port: 5004 },
    { name: 'SessionTrackerService', path: '../SessionTrackerService', port: 5179 },
    { name: 'SoundService', path: '../SoundService', port: 5014 }
  ];

  services.forEach(service => {
    const servicePath = path.join(__dirname, service.path);
    console.log(`Starting ${service.name}...`);
    
    const process = spawn('dotnet', ['run'], {
      cwd: servicePath,
      shell: true
    });

    process.stdout.on('data', (data) => {
      console.log(`[${service.name}] ${data}`);
    });

    process.stderr.on('data', (data) => {
      console.error(`[${service.name} ERROR] ${data}`);
    });

    backendProcesses.push(process);
  });

  // waiting for services to initialize
  return new Promise(resolve => setTimeout(resolve, 3000));
}

function stopBackendServices() {
  console.log('Stopping backend services...');
  backendProcesses.forEach(proc => {
    try {
      proc.kill();
    } catch (err) {
      console.error('Error killing process:', err);
    }
  });
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 900,
    height: 800,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    },
    backgroundColor: '#1a1a1a',
    title: 'Focus'
  });

  mainWindow.loadFile('index.html');
}

app.whenReady().then(async () => {
  await startBackendServices();
  createWindow();
});

app.on('window-all-closed', () => {
  stopBackendServices();
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', () => {
  stopBackendServices();
});

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});