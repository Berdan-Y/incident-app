// Ensure the window object exists
if (typeof window !== 'undefined') {
    window.createFileFromBytes = async function (bytes, fileName, contentType) {
        try {
            console.log('Creating file from bytes:', { fileName, contentType, bytesLength: bytes.length });
            
            // Create a Blob from the bytes
            const blob = new Blob([bytes], { type: contentType });
            
            // Create a File object from the Blob
            const file = new File([blob], fileName, { type: contentType });
            
            console.log('File created:', { name: file.name, size: file.size, type: file.type });
            
            // Return a simple object with the file data
            return {
                name: fileName,
                size: file.size,
                contentType: contentType,
                data: bytes
            };
        } catch (error) {
            console.error('Error creating file:', error);
            return null;
        }
    };

    // Store captured images
    let capturedImages = [];

    window.getCapturedImages = function() {
        return capturedImages;
    };

    window.setCapturedImages = function(images) {
        capturedImages = images;
    };

    window.removeCapturedImage = function(index) {
        if (index >= 0 && index < capturedImages.length) {
            capturedImages.splice(index, 1);
        }
    };
} 