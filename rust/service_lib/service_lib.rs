#![allow(dead_code, unused_variables)]


pub mod service {
    use custom_action::CustomAction;

    use std::sync::Arc;
    use std::time::{Duration, SystemTime};
    use tokio::sync::Mutex as AsyncMutex;
    use colored::*;

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
                    if action.get_usable() {
                        action.update(); // Update before execution
                        actions_to_run.push(action.clone()); // Clone only usable actions
                    }
                }
                drop(actions); // Explicitly release the lock before spawning tasks
        
                // Spawn tasks outside the lock
                for action in actions_to_run {
                    tokio::task::spawn(async move {
                        action.run();
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
        pub async fn find_action_by_id(&self, id: u64) -> Option<CustomAction> {
            let actions = self.actions.lock().await;
            actions.iter().find(|action| action.get_id() == id).cloned()
        }
        pub async fn find_action_by_name(&self, name: &str) -> Option<CustomAction> {
            let actions = self.actions.lock().await;
            actions.iter().find(|action| action.get_name() == name).cloned()
        }
        
        pub async fn status(&self) {
            let mut actions = self.actions.lock().await;

            for action in actions.iter_mut() {
                let status = if action.get_usable() { 
                    "Active".bold().green()
                } else { 
                    "Unactive".bold().red()
                };

                println!("{} ({}) - {status}", action.get_name(), action.get_id());
            }
        }
    }

    
    pub mod custom_action {
        // libs
        use std::sync::Arc;

        // custom type for async action
        type AsyncAction = Arc<dyn Fn() + Send + Sync>;

        // ARS - Action Repeat State
        #[derive(Clone)]
        pub enum Ars {
            Inf,
            Normal,
            Ended,
        }

        // Repeat save how much times the action repeat on self
        #[derive(Clone)]
        pub struct Repeat {
            times: u32,
            current: u32,
            state: Ars,
        }
        // builders
        impl Repeat {
            pub fn new(times: u32) -> Self {
                Self {
                    times,
                    current: 0,
                    state: Ars::Normal,
                }
            }
        
            pub fn inf() -> Self {
                Self {
                    times: 0,
                    current: 0,
                    state: Ars::Inf,
                }
            }
        }
        // getters
        impl Repeat {
            pub fn get_times(&self) -> u32 {
                self.times
            }

            pub fn get_current(&self) -> u32 {
                self.current
            }

            pub fn get_state(&self) -> &Ars {
                &self.state
            }
            pub fn set_state(&mut self, ars: Ars) {
                self.state = ars;
            }
        }
        // others
        impl Repeat {
            fn count(&mut self) {
                self.current = self.current.saturating_add(1);
            }

            fn ended(&mut self) {
                if let Ars::Normal = self.state {
                    if self.current >= self.times {
                        self.state = Ars::Ended;
                    }
                }
            }
        
            pub fn update (&mut self) {
                self.count();
                self.ended();
            }
        }

        pub struct CustomActionBuilder<F>
        where
            F: Fn() + Send + Sync + 'static,
        {
            name: String,
            description: String,
            action: Arc<F>,
            repeats: Option<u32>,
        }

        impl<F> CustomActionBuilder<F>
        where
            F: Fn() + Send + Sync + 'static,
        {
            pub fn new(action: F) -> Self {
                Self {
                    name: "Action".to_string(),
                    description: "Null".to_string(),
                    action: Arc::new(action),
                    repeats: None,
                }
            }

            pub fn name(mut self, name: &str) -> Self {
                self.name = name.to_string();
                self
            }

            pub fn description(mut self, description: &str) -> Self {
                self.description = description.to_string();
                self
            }

            pub fn repeats(mut self, repeats: u32) -> Self {
                self.repeats = Some(repeats);
                self
            }

            pub fn build(self) -> CustomAction {
                CustomAction {
                    name: self.name,
                    description: self.description,
                    action: self.action,
                    repeats: self.repeats.map_or_else(Repeat::inf, Repeat::new),

                    id: 0,
                    usable: true,
                    unusable: "Usable".to_string(),
                }
            }
        }

        // the info about the action and it self
        #[derive(Clone)]
        pub struct CustomAction {
            name: String,
            description: String,

            action: AsyncAction,
            repeats: Repeat,
            id: u64,

            usable: bool,
            unusable: String,
        }
        // geters + setters
        impl CustomAction {
            // Getter for name
            pub fn get_name(&self) -> &String {
                &self.name
            }
            // Setter for name
            pub fn set_name(&mut self, name: String) {
                self.name = name;
            }

            // Getter for description
            pub fn get_description(&self) -> &String {
                &self.description
            }
            // Setter for description
            pub fn set_description(&mut self, description: String) {
                self.description = description;
            }

            // Setter for ID
            pub fn get_id(&self) -> u64 {
                self.id
            }
            // Setter for ID
            pub fn set_id(&mut self, id: u64) {
                self.id = id;
            }

            // Getter for usable status
            pub fn get_usable(&self) -> bool {
                self.usable
            }
            // Setter for usable status
            pub fn set_usable(&mut self, usable: bool) {
                self.usable = usable;
            }

            // Getter for unusable
            pub fn get_unusable(&self) -> &String {
                &self.unusable
            }
            // Setter for unusable
            pub fn set_unusable(&mut self, unusable: String) {
                self.unusable = unusable;
            }
        }
        // others
        impl CustomAction {
            pub fn update (&mut self) {
                self.repeats.update();

                if let Ars::Ended = self.repeats.state {
                    self.usable = false;
                    self.unusable = "Run out of repeats".to_string();
                }
            }
            pub fn run(&self) {
                (self.action)();
            }

            pub fn repeats(&self) -> u32 {
                std::cmp::min(self.repeats.get_current() , 999_999_999)
            }
            pub fn repeats_left(&self) -> u32 {
                let left = self.repeats.get_times() as i32 - self.repeats.get_current() as i32;

                if left < 0 {
                    999_999_999
                } else {
                    left as u32 // Convert back if needed
                }
                
            }
            pub fn repeats_state(&self) -> &Ars {
                &self.repeats.get_state()
            }

        }
    }
    
}