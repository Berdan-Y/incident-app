window.startCamera = async function () {
    const video = document.getElementById('video');
    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ video: true });
            video.srcObject = stream;
            video.play();
        } catch (err) {
            alert('Error accessing camera: ' + err.message);
        }
    } else {
        alert('Camera API not supported by this browser.');
    }
};

window.takePhoto = function () {
    try {
        const video = document.getElementById('video');
        if (!video) throw new Error('Video element not found');
        const canvas = document.createElement('canvas');
        canvas.width = 320;
        canvas.height = 240;

        const context = canvas.getContext('2d');
        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        const dataUrl = canvas.toDataURL('image/jpeg');
        return dataUrl;
    } catch (err) {
        console.error('Error taking photo:', err);
        alert('Error taking photo: ' + err.message);
        return null;
    }
};