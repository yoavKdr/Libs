#![allow(dead_code, unused_variables)]

pub mod service_lib {
    use custom_action::CustomAction;

    use std::sync::Arc;
    use std::time::{Duration, SystemTime};
    use tokio::sync::Mutex as AsyncMutex;

    pub struct BackgroundService {
        actions: Arc<AsyncMutex<Vec<CustomAction>>>,
        interval: Duration,
        stop_signal: Arc<AsyncMutex<bool>>,
    }

    // init
    impl BackgroundService {
        pub fn new(interval: Duration) -> Self {
            Self {
                actions: Arc::new(AsyncMutex::new(Vec::new())),
                interval,
                stop_signal: Arc::new(AsyncMutex::new(false)),
            }
        }
    }
    
    impl BackgroundService {
        pub async fn start(&self) {
            self.unstop().await;
            loop {
                let start_time = SystemTime::now(); // Capture the start time

                let mut actions = self.actions.lock().await;
        
                // Collect only usable actions first
                let mut actions_to_run: Vec<CustomAction> = Vec::new();
                for action in actions.iter_mut() {
                    if action.usable {
                        action.update(); // Update before execution
                        actions_to_run.push(action.clone()); // Clone only usable actions
                    }
                }
                drop(actions); // Explicitly release the lock before spawning tasks
        
                // Spawn tasks outside the lock
                for action in actions_to_run {
                    tokio::task::spawn(async move {
                        (action.action)();
                    });
                }
                

                // Sleep for the interval duration before running again
                let elapsed = start_time.elapsed().unwrap();
                if self.interval > elapsed {
                    tokio::time::sleep(self.interval - elapsed).await;
                }
                
                // Check if we need to stop
                let stop_signal = self.stop_signal.lock().await;
                if *stop_signal {
                    break;
                }
            }
        }

        pub async fn stop(&self) {
            let mut stop_signal = self.stop_signal.lock().await;
            *stop_signal = true;
        }

        pub async fn unstop(&self) {
            let mut stop_signal = self.stop_signal.lock().await;
            *stop_signal = false;
        }

        pub async fn add_action(&self, mut action: CustomAction) {
            let mut actions = self.actions.lock().await;
            action.set_id(actions.len() as u64 + 1);
            actions.push(action);
        }
    }

    
    pub mod custom_action {
        use std::sync::Arc;

        type AsyncAction = Arc<dyn Fn() + Send + Sync>;

        // ARS - Action Repeat State
        #[derive(Clone)]
        enum Ars {
            Inf,
            Normal,
            Ended,
        }
        #[derive(Clone)]
        pub struct Repeat {
            times: u32,
            current: u32,
            state: Ars,
        }
        impl Repeat {
            fn new(times: u32) -> Self {
                Self {
                    times,
                    current: 0,
                    state: Ars::Normal,
                }
            }
          
            fn inf() -> Self {
                Self {
                    times: 0,
                    current: 0,
                    state: Ars::Inf,
                }
            }
        }
        impl Repeat {
            fn count(&mut self) {
                self.current = self.current.saturating_add(1);
            }

            fn ended(&mut self) -> bool {
                match self.state {
                    Ars::Inf => return false,
                    _ => {
                        if self.times < self.current + 1  {
                            self.state = Ars::Ended;
                            return true;
                        }
                        return false;
                    }
                }
            }
        }

        #[derive(Clone)]
        pub struct CustomAction {
            pub action: AsyncAction,
            pub repeats: Repeat,
            pub id: u64,

            pub usable: bool,
        }
        impl CustomAction {
            pub fn new<F>(action: F, repeats: Option<u32>) -> Self
            where
                F: Fn() + Send + Sync + 'static,
            {
                Self {
                    action: Arc::new(action),
                    repeats: match repeats {
                        Some(r) => Repeat::new(r),
                        None => Repeat::inf(),
                    },
                    id: 0,

                    usable: true,
                }
            }

            pub fn update (&mut self) {
                self.repeats.count();
                if self.repeats.ended() {
                    self.usable = false;
                }

                //print!("usable is: {:?}", self.usable);
            }

            pub fn set_id(&mut self, id: u64) {
                self.id = id;
            }
            pub fn get_id(&self) -> u64 {
                return self.id;
            }
        }
    }
}