mod service_lib;
mod server_lib;

use std::{sync::Arc, time::{Duration, SystemTime, UNIX_EPOCH}};
use service_lib::service_lib::{custom_action::CustomAction, BackgroundService};

#[tokio::main]
async fn main() {
    let service = Arc::new(BackgroundService::new(Duration::from_secs(1)));

    let ca = CustomAction::new(|| hello(), Some(6));
    service.add_action(ca).await;

    let service_clone = Arc::clone(&service);

    // Spawn start() in the background
    let _start_task = tokio::spawn(async move {
        service_clone.start().await;
    });

    tokio::time::sleep(Duration::from_secs(2)).await;
    service.stop().await; // Stop service after 2 seconds

    tokio::time::sleep(Duration::from_secs(4)).await;

    let ca = CustomAction::new(|| println!("test"), Some(3));
    service.add_action(ca).await;

    let service_clone = Arc::clone(&service);

    // Start service again
    let _start_task = tokio::spawn(async move {
        service_clone.start().await;
    });

    // Wait for Ctrl+C signal
    tokio::signal::ctrl_c().await.expect("Failed to listen for Ctrl+C");
    println!("Stopping service...");
}

fn hello() {
    let now = SystemTime::now().duration_since(UNIX_EPOCH).unwrap();
    let seconds = now.as_secs() % 60; // Get only the seconds part

    println!("hello at: {}", seconds);
}
