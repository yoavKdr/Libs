mod service_lib;
mod server_lib;

#[allow(unused_imports)]
use std::{sync::Arc, time::{Duration, SystemTime, UNIX_EPOCH}};
#[allow(unused_imports)]
use service_lib::service::{custom_action::{CustomAction, CustomActionBuilder}, BackgroundService};

#[tokio::main]
async fn main() {
    let service = Arc::new(BackgroundService::new(Duration::from_secs(1)));

    let ca = CustomActionBuilder::new(|| hello()).name("First").build();
    service.add_action(ca).await;

    let service_clone = Arc::clone(&service);

    // Spawn start() in the background
    let _start_task = tokio::spawn(async move {
        service_clone.start().await;
    });

    tokio::time::sleep(Duration::from_secs(2)).await;

    let ca = CustomActionBuilder::new(|| hello()).name("secend").build();
    service.add_action(ca).await;


    let service_clone = Arc::clone(&service);
    tokio::time::sleep(Duration::from_secs(6)).await;
    service_clone.status().await;


    // Wait for Ctrl+C signal
    tokio::signal::ctrl_c().await.expect("Failed to listen for Ctrl+C");
    println!("Stopping service...");
}

fn hello() {
    //let now = SystemTime::now().duration_since(UNIX_EPOCH).unwrap();
    //let seconds = now.as_secs() % 60; // Get only the seconds part
    //println!("send at: {}", seconds);
    return;
}
