console.log("✅ renderTaskCharts loaded");
window.renderTaskCharts = (open, inProgress, done) => {
    console.log("✅ renderTaskCharts called", open, inProgress, done);

    // ⏳ attendre que le layout soit complètement prêt
    
    setTimeout(() => {
        

        const pieCanvas = document.getElementById('tasksPieChart');
        const barCanvas = document.getElementById('tasksBarChart');

        if (!pieCanvas || !barCanvas) {
            console.error("Canvas not found");
            return;
        }

        // Nettoyage si déjà rendu (navigation)
        if (pieCanvas.chart) pieCanvas.chart.destroy();
        if (barCanvas.chart) barCanvas.chart.destroy();

        pieCanvas.chart = new Chart(pieCanvas, {
            type: 'pie',
            data: {
                labels: ['Open', 'In Progress', 'Done'],
                datasets: [{
                    data: [open, inProgress, done],
                    backgroundColor: ['#ffc107', '#0dcaf0', '#198754']
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false
            }
        });

        barCanvas.chart = new Chart(barCanvas, {
            type: 'bar',
            data: {
                labels: ['Open', 'In Progress', 'Done'],
                datasets: [{
                    label: 'Tasks',
                    data: [open, inProgress, done],
                    backgroundColor: ['#ffc107', '#0dcaf0', '#198754']
                }]
            },
            options: {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: { display: false }
    },
    scales: {
        y: {
            beginAtZero: true,
            ticks: {
                stepSize: 1
            },
            max: Math.max(open, inProgress, done) + 1
        }
    }
}
        });

    }, 300); // ✅ délai clé
};
